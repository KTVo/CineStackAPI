using System.Reflection;
using CineStackAPI.MVCS.Models.Validation;

namespace CineStackAPI.Helpers.Validation;
public static class ModelValidationHelpers
{
    /// <summary>
    /// NULL CHECKS CLASS VARIABLES
    /// </summary>
    /// <param name="objectsToCheck"></param>
    /// <returns></returns>
    public static ModelValidationResponse Validate<T>(T model)
    {
        ModelValidationResponse response = new()
        {
            IsSuccess = true,
            Errors = new List<string>()
        };

        if (model is null)
        {
            response.IsSuccess = false;
            response.Message = "MODEL IS NULL";
            response.Errors.Add($"{typeof(T).Name} IS NULL!");

            return response;
        }

        PropertyInfo[] properties = typeof(T).GetProperties();

        foreach (PropertyInfo property in properties)
        {
            object? value = property.GetValue(model);

            if (value is null)
            {
                response.Errors.Add($"{property.Name} IS NULL!");
                continue;
            }

            if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                response.Errors.Add($"{property.Name} IS EMPTY!");
            }
        }

        if (response.Errors.Count > 0)
        {
            response.IsSuccess = false;
            response.Message = "MODEL VALIDATION FAILED!";
        }

        return response;
    }
}