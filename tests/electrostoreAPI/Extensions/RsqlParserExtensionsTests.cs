using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Xunit;

namespace ElectrostoreAPI.Tests.Extensions
{
    public class RsqlParserExtensionsTests
    {
        private static Func<T, bool> Compile<T>(params FilterDto[] filters)
        {
            var (expression, _) = RsqlParserExtensions.ToFilterExpression<T>(filters.ToList());
            return expression.Compile();
        }

        [Fact]
        public void ToFilterExpression_ShouldMatchNullableIntColumn_WithEqFilter()
        {
            var matches = Compile<CommandsComments>(new FilterDto { field = "id_user", search_type = "eq", value = "5" });

            Assert.True(matches(new CommandsComments { id_user = 5, content_command_comment = "a" }));
            Assert.False(matches(new CommandsComments { id_user = 6, content_command_comment = "a" }));
            Assert.False(matches(new CommandsComments { id_user = null, content_command_comment = "a" }));
        }

        [Fact]
        public void ToFilterExpression_ShouldSupportComparisonsOnNullableColumns()
        {
            var greater = Compile<CommandsComments>(new FilterDto { field = "id_user", search_type = "gt", value = "5" });
            var notEqual = Compile<CommandsComments>(new FilterDto { field = "id_user", search_type = "ne", value = "5" });

            Assert.True(greater(new CommandsComments { id_user = 6, content_command_comment = "a" }));
            Assert.False(greater(new CommandsComments { id_user = 5, content_command_comment = "a" }));
            Assert.True(notEqual(new CommandsComments { id_user = 6, content_command_comment = "a" }));
            Assert.False(notEqual(new CommandsComments { id_user = 5, content_command_comment = "a" }));
        }

        [Fact]
        public void ToFilterExpression_ShouldSkipFilter_WhenValueCannotBeConverted()
        {
            var matches = Compile<CommandsComments>(new FilterDto { field = "id_user", search_type = "eq", value = "not-a-number" });

            Assert.True(matches(new CommandsComments { id_user = 1, content_command_comment = "a" }));
        }

        [Fact]
        public void ToFilterExpression_ShouldMatchNonNullableColumns()
        {
            var matches = Compile<Projects>(new FilterDto { field = "id_project", search_type = "eq", value = "3" });

            Assert.True(matches(new Projects { id_project = 3, name_project = "p" }));
            Assert.False(matches(new Projects { id_project = 4, name_project = "p" }));
        }

        [Fact]
        public void ToFilterExpression_ShouldParseEnumColumns_CaseInsensitively()
        {
            var matches = Compile<Projects>(new FilterDto { field = "status_project", search_type = "eq", value = "inprogress" });

            Assert.True(matches(new Projects { name_project = "p", status_project = ProjectStatus.InProgress }));
            Assert.False(matches(new Projects { name_project = "p", status_project = ProjectStatus.Completed }));
        }

        [Fact]
        public void ToFilterExpression_ShouldMatchSubstrings_WithLikeFilter()
        {
            var matches = Compile<Projects>(new FilterDto { field = "name_project", search_type = "like", value = "tor" });

            Assert.True(matches(new Projects { name_project = "monitor" }));
            Assert.False(matches(new Projects { name_project = "cable" }));
        }

        [Fact]
        public void ToFilterExpression_ShouldReturnAlwaysTrue_WhenNoFilterProvided()
        {
            var (expression, remaining) = RsqlParserExtensions.ToFilterExpression<Projects>(null);

            Assert.True(expression.Compile()(new Projects { name_project = "x" }));
            Assert.Empty(remaining!);
        }
    }
}
