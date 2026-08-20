using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EduFlowAI.Admission.Application.Extensions
{
    public static class EnumDisplayExtensions
    {
        /// <summary>
        /// Retrieves the Name and Description from the [Display] attribute of an Enum value.
        /// </summary>
        public static (string Name, string Description) GetDisplayInfo(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return (value.ToString(), string.Empty);

            var attribute = field.GetCustomAttribute<DisplayAttribute>();

            return attribute == null
                ? (value.ToString(), string.Empty)
                : (attribute.Name ?? value.ToString(), attribute.Description ?? string.Empty);
        }
    }
}
