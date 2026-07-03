using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HomeScoutCopilot.API.Service;

/// <summary>
/// Options that declare their own config section and a FluentValidation validator, so
/// they can be bound and validated on startup with <see cref="ValidatedOptionsExtensions.AddValidatedOptions{TOptions}"/>.
/// </summary>
public interface IValidatedOptions<TOptions>
    where TOptions : class, IValidatedOptions<TOptions>, new()
{
    static abstract string SectionName { get; }

    IValidator<TOptions> GetValidator();
}

public static class ValidatedOptionsExtensions
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> from its declared section and validates it
    /// (FluentValidation) on startup — bad config fails fast. Returns the builder so the
    /// caller can chain <c>PostConfigure</c>.
    /// </summary>
    public static OptionsBuilder<TOptions> AddValidatedOptions<TOptions>(
        this IServiceCollection services, IConfiguration configuration)
        where TOptions : class, IValidatedOptions<TOptions>, new()
    {
        services.AddSingleton<IValidateOptions<TOptions>, ValidatedOptionsValidator<TOptions>>();
        return services.AddOptions<TOptions>()
            .Bind(configuration.GetSection(TOptions.SectionName))
            .ValidateOnStart();
    }
}

internal sealed class ValidatedOptionsValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class, IValidatedOptions<TOptions>, new()
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var result = options.GetValidator().Validate(options);
        return result.IsValid
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(result.Errors.Select(failure => failure.ErrorMessage));
    }
}
