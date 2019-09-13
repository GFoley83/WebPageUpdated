using FluentValidation;

namespace WebPageUpdated
{
    public class AddWebPageUpdatedJobValidator : AbstractValidator<AddWebPageUpdatedJobDto>
    {
        public AddWebPageUpdatedJobValidator()
        {
            RuleFor(x => x.WebPageUrl).NotEmpty().Must(url =>
            {
                return url.IndexOf("http://") > -1 || url.IndexOf("https://") > -1;
            });

            RuleFor(x => x.Email).NotEmpty().EmailAddress();

            //When(x => string.IsNullOrWhiteSpace(x.XpathOfElementToWatch) && string.IsNullOrWhiteSpace(x.CssPathOfElementToWatch), () =>
            //{
            //    RuleFor(x => x.CssPathOfElementToWatch).NotEmpty().When(x => string.IsNullOrWhiteSpace(x.XpathOfElementToWatch));
            //    RuleFor(x => x.XpathOfElementToWatch).NotEmpty().When(x => string.IsNullOrWhiteSpace(x.CssPathOfElementToWatch));
            //});

            When(x => !string.IsNullOrWhiteSpace(x.PathOfElementToWatch), () =>
            {
                RuleFor(x => x.PathOfElementToWatch)
                    .NotEmpty()
                    .Must(selector =>
                    {
                        var sel = WebPageService.GetSelectorType(selector);
                        return sel != SelectorType.Unspecified;
                    })
                    .WithMessage(i => "Please include a valid css or xpath selector.");
            });
        }
    }
}
