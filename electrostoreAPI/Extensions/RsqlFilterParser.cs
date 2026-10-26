using ElectrostoreAPI.Dto;
using System.Text;

namespace ElectrostoreAPI.Extensions;

/// <summary>
/// Parses a RSQL filter: conditions combined with ';' (and) and ',' (or), 'and' binds tighter than 'or',
/// parentheses group conditions. A condition is selector operator value, "=in=" takes a list of values
/// in parentheses and is read as an or of "==".
/// A value containing one of ; , ( ) ' " must be quoted with ' or ", a backslash escapes a character of a quoted value.
/// A malformed condition is skipped, the other ones are kept.
/// </summary>
internal sealed class RsqlFilterParser
{
    // the longest token of an operator comes first
    private static readonly (string Token, string SearchType)[] Operators =
    [
        ("!=null=", "notnull"), ("=null=", "null"), ("==", "eq"), ("!=", "ne"),
        ("=gt=", "gt"), ("=lt=", "lt"), ("=ge=", "ge"), ("=le=", "le"),
        ("=like=", "like"), ("=in=", "in"),
    ];

    private readonly string _input;
    private int _pos;

    private RsqlFilterParser(string input)
    {
        _input = input;
    }

    private bool AtEnd => _pos >= _input.Length;

    private char Current => _input[_pos];

    /// <summary>
    /// The conditions to satisfy all together, a condition is a group if it combines several ones with "or".
    /// </summary>
    public static List<FilterDto> Parse(string rsql)
    {
        var root = new RsqlFilterParser(rsql).ParseOr(0);
        if (root == null)
        {
            return [];
        }
        return root.search_type == "and" ? root.children! : [root];
    }

    private static FilterDto? Combine(string searchType, IEnumerable<FilterDto> terms)
    {
        // (a;b);c is a;b;c
        var flat = terms.SelectMany(term => term.search_type == searchType ? term.children! : [term]).ToList();
        return flat.Count switch
        {
            0 => null,
            1 => flat[0],
            _ => searchType == "and" ? FilterDto.And(flat) : FilterDto.Or(flat),
        };
    }

    private bool Consume(char c)
    {
        if (AtEnd || Current != c)
        {
            return false;
        }
        _pos++;
        return true;
    }

    private void SkipWhiteSpaces()
    {
        while (!AtEnd && char.IsWhiteSpace(Current))
        {
            _pos++;
        }
    }

    private FilterDto? ParseOr(int depth)
    {
        var terms = new List<FilterDto>();
        do
        {
            var term = ParseAnd(depth);
            if (term != null)
            {
                terms.Add(term);
            }
        } while (Consume(','));
        return Combine("or", terms);
    }

    private FilterDto? ParseAnd(int depth)
    {
        var terms = new List<FilterDto>();
        do
        {
            var term = ParseConstraint(depth);
            if (term != null)
            {
                terms.Add(term);
            }
        } while (Consume(';'));
        return Combine("and", terms);
    }

    private FilterDto? ParseConstraint(int depth)
    {
        SkipWhiteSpaces();
        FilterDto? constraint;
        if (Consume('('))
        {
            constraint = ParseOr(depth + 1);
            Consume(')');
        }
        else
        {
            constraint = ParseCondition(depth);
        }
        // what is left of a malformed condition
        SkipToDelimiter(depth);
        return constraint;
    }

    private FilterDto? ParseCondition(int depth)
    {
        var selector = ReadSelector();
        if (selector == null)
        {
            return null;
        }
        var index = Array.FindIndex(Operators, o => string.CompareOrdinal(_input, _pos, o.Token, 0, o.Token.Length) == 0);
        if (index < 0)
        {
            return null;
        }
        _pos += Operators[index].Token.Length;
        var searchType = Operators[index].SearchType;
        if (searchType == "in")
        {
            return ParseIn(selector, depth);
        }
        return Condition(selector, searchType, ReadValue(depth, false));
    }

    /// <summary>
    /// field=in=(a,b) is field==a,field==b
    /// </summary>
    private FilterDto? ParseIn(string selector, int depth)
    {
        var values = new List<string>();
        if (Consume('('))
        {
            do
            {
                values.Add(ReadValue(depth, true));
            } while (Consume(','));
            if (!Consume(')'))
            {
                return null;
            }
        }
        else
        {
            values.Add(ReadValue(depth, false));
        }
        return Combine("or", values.Select(value => Condition(selector, "eq", value)));
    }

    private static FilterDto Condition(string selector, string searchType, string value)
    {
        return new FilterDto { field = selector, search_type = searchType, value = value };
    }

    /// <summary>
    /// The field of a condition, an aggregate such as SUM(ItemsBoxs.quantity_item_box) contains parentheses.
    /// </summary>
    private string? ReadSelector()
    {
        var start = _pos;
        while (!AtEnd && Current != '=' && Current != '!')
        {
            if (Current is ';' or ',' or ')')
            {
                return null;
            }
            if (Current == '(')
            {
                SkipParentheses();
            }
            else
            {
                _pos++;
            }
        }
        var selector = _input[start.._pos].Trim();
        return selector.Length == 0 ? null : selector;
    }

    private void SkipParentheses()
    {
        var nested = 0;
        while (!AtEnd)
        {
            if (Current == '(')
            {
                nested++;
            }
            else if (Current == ')')
            {
                nested--;
            }
            _pos++;
            if (nested == 0)
            {
                return;
            }
        }
    }

    /// <summary>
    /// A quoted value, or what is left until a delimiter. ')' is one in a group or in a list of values.
    /// </summary>
    private string ReadValue(int depth, bool inList)
    {
        if (inList)
        {
            SkipWhiteSpaces();
        }
        if (!AtEnd && Current is '\'' or '"')
        {
            return ReadQuoted();
        }
        var start = _pos;
        while (!AtEnd && Current != ';' && Current != ',' && !(Current == ')' && (inList || depth > 0)))
        {
            _pos++;
        }
        var value = _input[start.._pos];
        return inList ? value.TrimEnd() : value;
    }

    private string ReadQuoted()
    {
        var quote = Current;
        _pos++;
        var value = new StringBuilder();
        while (!AtEnd && Current != quote)
        {
            if (Current == '\\' && _pos + 1 < _input.Length)
            {
                _pos++;
            }
            value.Append(Current);
            _pos++;
        }
        // closing quote
        _pos++;
        return value.ToString();
    }

    /// <summary>
    /// Stops before the next ';' or ',' of the current level, or before the ')' that ends the group.
    /// </summary>
    private void SkipToDelimiter(int depth)
    {
        var nested = 0;
        while (!AtEnd)
        {
            var c = Current;
            if (c is '\'' or '"')
            {
                ReadQuoted();
                continue;
            }
            if (c == '(')
            {
                nested++;
            }
            else if (c == ')')
            {
                if (nested > 0)
                {
                    nested--;
                }
                else if (depth > 0)
                {
                    return;
                }
            }
            else if (nested == 0 && c is ';' or ',')
            {
                return;
            }
            _pos++;
        }
    }
}
