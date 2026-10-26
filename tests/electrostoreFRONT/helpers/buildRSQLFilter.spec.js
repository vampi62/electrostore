import { describe, it, expect } from 'vitest'
import { buildRSQLFilter } from '@/helpers/buildRSQLFilter'
import { matchFilter } from '@/helpers/filterValue'

const tagFilter = (value, partialValues) => ({ key: 'ItemsTags.Tag.name_tag', compareMethod: '=in=', value, partialValues })

describe('buildRSQLFilter', () => {
  it('builds nothing for a filter without value', () => {
    expect(buildRSQLFilter([tagFilter([], []), { key: 'a', compareMethod: '==', value: '' }])).toBe('')
  })

  it('joins the conditions of the filters with ;', () => {
    expect(buildRSQLFilter([
      { key: 'a', compareMethod: '=ge=', value: 1 },
      { key: 'b', compareMethod: '=like=', value: 'x' },
    ])).toBe('a=ge=1;b=like=x')
  })

  it('builds a single =in= condition with the values in parentheses', () => {
    expect(buildRSQLFilter([tagFilter(['a', 'b'], [])])).toBe('ItemsTags.Tag.name_tag=in=(a,b)')
  })

  it('builds the =like= of the partial values alone', () => {
    expect(buildRSQLFilter([tagFilter([], ['res'])])).toBe('ItemsTags.Tag.name_tag=like=res')
  })

  it('puts the =in= and the =like= in a group separated by ,', () => {
    expect(buildRSQLFilter([tagFilter(['a', 'b'], ['res', 'cap'])]))
      .toBe('(ItemsTags.Tag.name_tag=in=(a,b),ItemsTags.Tag.name_tag=like=res,ItemsTags.Tag.name_tag=like=cap)')
  })

  it('ignores the partial values of a filter that is not =in=', () => {
    expect(buildRSQLFilter([{ key: 'a', compareMethod: '==', value: 'x', partialValues: ['y'] }])).toBe('a==x')
  })

  it('quotes the values that contain a delimiter', () => {
    expect(buildRSQLFilter([tagFilter(['a,b', "it's"], ['res (1k)'])]))
      .toBe("(ItemsTags.Tag.name_tag=in=('a,b','it\\'s'),ItemsTags.Tag.name_tag=like='res (1k)')")
    expect(buildRSQLFilter([{ key: 'a', compareMethod: '=like=', value: 'x;y' }])).toBe("a=like='x;y'")
  })
})

describe('matchFilter', () => {
  const row = (names) => () => names

  it('matches a row with one of the values', () => {
    expect(matchFilter(tagFilter(['a', 'b'], []), row(['b', 'c']))).toBe(true)
    expect(matchFilter(tagFilter(['a'], []), row(['b', 'c']))).toBe(false)
  })

  it('matches a row that contains a partial value', () => {
    expect(matchFilter(tagFilter([], ['res']), row(['Resistor']))).toBe(true)
    expect(matchFilter(tagFilter([], ['cap']), row(['Resistor']))).toBe(false)
  })

  it('matches a row with a value or a partial value', () => {
    expect(matchFilter(tagFilter(['a'], ['res']), row(['Resistor']))).toBe(true)
    expect(matchFilter(tagFilter(['a'], ['res']), row(['a']))).toBe(true)
    expect(matchFilter(tagFilter(['a'], ['res']), row(['b']))).toBe(false)
  })

  it('matches every row without value', () => {
    expect(matchFilter(tagFilter([], []), row(['b']))).toBe(true)
  })
})
