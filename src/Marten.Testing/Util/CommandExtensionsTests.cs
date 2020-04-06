namespace Marten.Testing.Util
{
    using System.Data;

    using Marten.Util;

    using Microsoft.Data.SqlClient;

    using Shouldly;

    using Xunit;

    public class CommandExtensionsTests
    {
        [Fact]
        public void add_first_parameter()
        {
            var command = new SqlCommand();

            var param = command.AddParameter("a");

            param.Value.ShouldBe("a");
            param.ParameterName.ShouldBe("arg0");

            param.SqlDbType.ShouldBe(SqlDbType.Text);

            command.Parameters.Contains(param).ShouldBeTrue();
        }

        [Fact]
        public void add_second_parameter()
        {
            var command = new SqlCommand();

            command.AddParameter("a");
            var param = command.AddParameter("b");

            param.ParameterName.ShouldBe("arg1");
        }
    }
}
