using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using AutoFixture;
using FluentAssertions;
using Xunit;

namespace SapNwRfc.Tests
{
    public sealed class SapConnectionParametersTests
    {
        public static IEnumerable<object[]> ConnectionStringTestCases =>
            new List<object[]>
            {
                new object[] { "AppServerHost=q1.sap.test;SystemNumber=1;User=user;Password={dummypassword};Client=999;Language=en;PoolSize=2;Trace=8;Name=Q1;ProgramId=myapp" },
                new object[] { "AppServerHost=q1.sap.test;SystemNumber=1;User=user;Client=999;Language=en;PoolSize=2;Trace=8;Name=Q1;ProgramId=myapp" },
                new object[] { "NOT_EXIST_PARAM=q1.sap.test;SystemNumber=1;User=user;Client=999;Language=en;PoolSize=2;Trace=8;Name=Q1;ProgramId=myapp" },
            };

        private static readonly Fixture Fixture = new Fixture();

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Parse_InvalidConnectionString_ShouldThrowArgumentException(string connectionString)
        {
            // Act
            Action action = () => SapConnectionParameters.Parse(connectionString);

            // Assert
            action.Should().Throw<ArgumentException>()
                .Which.ParamName.Should().Be("connectionString");
        }

        [Fact]
        public void Parse_ShouldSetProperties()
        {
            // Arrange
            const string connectionString = "AppServerHost=MyFancyHost;User= SomeUsername; Password = SomePassword ";

            // Act
            var parameters = SapConnectionParameters.Parse(connectionString);

            // Assert
            parameters.Should().NotBeNull();
            parameters.AppServerHost.Should().Be("MyFancyHost");
            parameters.User.Should().Be("SomeUsername");
            parameters.Password.Should().Be("SomePassword");
        }

        [Fact]
        public void Parse_ShouldSupportEqualSignInPassword_Issue96()
        {
            // Arrange
            const string connectionString = "Password=my=password";

            // Act
            var parameters = SapConnectionParameters.Parse(connectionString);

            // Assert
            parameters.Should().NotBeNull();
            parameters.Password.Should().Be("my=password");
        }

        [Fact]
        public void Parse_AllProperties()
        {
            // Arrange
            SapConnectionParameters expectedParameters = Fixture.Create<SapConnectionParameters>();
            string connectionString = typeof(SapConnectionParameters)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Aggregate(new StringBuilder(), (sb, propertyInfo) =>
                {
                    object value = propertyInfo.GetValue(expectedParameters);
                    sb.Append($"{propertyInfo.Name}={value};");
                    return sb;
                })
                .ToString();

            // Act
            var parameters = SapConnectionParameters.Parse(connectionString);

            // Assert
            parameters.Should().BeEquivalentTo(expectedParameters);
        }

        [Theory]
        [MemberData(nameof(ConnectionStringTestCases))]
        public void TestParseAndToString(string connectionString)
        {
            try
            {
                var parameters = SapConnectionParameters.Parse(connectionString);
                var resultConnectionString = parameters.ToString();
                var reparsedParameters = SapConnectionParameters.Parse(resultConnectionString);
                reparsedParameters.Should().BeEquivalentTo(parameters);
            }
            catch (Exception ex)
            {
                connectionString.Contains("NOT_EXIST_PARAM").Should().BeTrue();
                if (connectionString.Contains("NOT_EXIST_PARAM"))
                    return;
                Console.WriteLine(ex);
                throw;
            }
        }
    }
}
