using FluentAssertions;
using QuickAuth.WebApi.Domain.ValueOfObjects;

namespace QuickAuth.Tests.Unit;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("871.263.170-29")]
    [InlineData("87126317029")]
    [InlineData("153.509.460-56")]
    [InlineData("15350946056")]
    public void IsValidCpf_ValidCpf_ReturnsTrue(string cpf)
    {
        cpf.IsValidCpf().Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsValidCpf_NullOrEmpty_ReturnsFalse(string? cpf)
    {
        cpf.IsValidCpf().Should().BeFalse();
    }

    [Theory]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    [InlineData("99999999999")]
    public void IsValidCpf_AllSameDigits_ReturnsFalse(string cpf)
    {
        cpf.IsValidCpf().Should().BeFalse();
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234567890")]
    [InlineData("123456789012")]
    public void IsValidCpf_WrongLength_ReturnsFalse(string cpf)
    {
        cpf.IsValidCpf().Should().BeFalse();
    }

    [Theory]
    [InlineData("529.982.247-26")]
    [InlineData("529.982.247-00")]
    [InlineData("111.444.777-36")]
    public void IsValidCpf_WrongCheckDigits_ReturnsFalse(string cpf)
    {
        cpf.IsValidCpf().Should().BeFalse();
    }
}
