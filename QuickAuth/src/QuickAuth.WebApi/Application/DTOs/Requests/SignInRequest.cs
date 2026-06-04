using System.ComponentModel.DataAnnotations;

namespace QuickAuth.WebApi.Application.DTOs.Requests;

public class SignInRequest
{
    [Required(ErrorMessage = "The CPF field is required.")]
    public string Cpf { get; set; } = string.Empty;
}