using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Globalization;

namespace RestoranYonetim.Helpers
{
    /// <summary>
    /// Decimal deðerleri hem nokta (.) hem virgül (,) ile kabul eden özel Model Binder
    /// </summary>
    public class DecimalModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            if (string.IsNullOrEmpty(value))
            {
                return Task.CompletedTask;
            }

            // Hem nokta hem virgülü kabul et
            decimal result;
            
            // Önce InvariantCulture ile dene (nokta ile)
            if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // Sonra tr-TR ile dene (virgül ile)
            if (decimal.TryParse(value, NumberStyles.Number, new CultureInfo("tr-TR"), out result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                return Task.CompletedTask;
            }

            // Hiçbiri iþe yaramazsa hata
            bindingContext.ModelState.TryAddModelError(
                modelName,
                $"'{value}' geçerli bir sayý deðil.");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Model Binder Provider - Decimal tipler için özel binder'ý etkinleþtirir
    /// </summary>
    public class DecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(decimal) ||
                context.Metadata.ModelType == typeof(decimal?))
            {
                return new DecimalModelBinder();
            }

            return null;
        }
    }
}
