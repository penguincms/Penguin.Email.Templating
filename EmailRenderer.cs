using Penguin.Email.Templating.Abstractions.Interfaces;
using Penguin.Templating.Abstractions;
using Penguin.Templating.Abstractions.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Penguin.Email.Templating
{
    /// <summary>
    /// An email renderer/binder that does not rely on MVC. Binds to {Parameter.Property} Notation
    /// </summary>
    public class EmailRenderer : IEmailTemplateRenderer
    {
        private static readonly List<Type> CopyTypes = new()
        {
            typeof(string),
            typeof(DateTime)
        };

        /// <summary>
        /// Constructs a new instance of this email renderer
        /// </summary>
        public EmailRenderer()
        {
        }

        /// <summary>
        /// Checks to see if the data type gets copied 1-1, or if recursive binding is needed
        /// </summary>
        /// <param name="toTest">The type to test</param>
        /// <returns>True if no recursive binding is needed</returns>
        public static bool IsStraightCopy(Type toTest)
        {
            return toTest is null ? throw new ArgumentNullException(nameof(toTest)) : toTest.IsValueType || CopyTypes.Contains(toTest);
        }

        /// <summary>
        /// Renders out the field of an email template into an HTML string using {Parameter.Property} notation
        /// </summary>
        /// <param name="Parameters">The source parameters as a dictionary of property name, value pairs</param>
        /// <param name="Template">The template to use as the binding definition</param>
        /// <param name="Field">The field of the email template that will be bound to (incase its not the body)</param>
        /// <returns>The HTML contents of the post bound template field</returns>
        public string RenderEmail(IEnumerable<TemplateParameter> Parameters, IEmailTemplate Template, PropertyInfo Field)
        {
            if (Parameters is null)
            {
                throw new ArgumentNullException(nameof(Parameters));
            }

            if (Field is null)
            {
                throw new ArgumentNullException(nameof(Field));
            }

            string TemplateValue = Field.GetValue(Template)?.ToString();

            foreach (TemplateParameter parameter in Parameters)
            {
                if (IsStraightCopy(parameter.Type))
                {
                    string thisMacro = "{" + $"{parameter.Name}" + "}";

                    string replacement = parameter.Value.ToString();

                    Field.SetValue(Template, Field.GetValue(Template)?.ToString().Replace(thisMacro, replacement));
                }
                else
                {
                    foreach (PropertyInfo objectProperty in parameter.Type.GetProperties().Where(p => p.GetCustomAttribute<DontTemplateBindAttribute>() is null))
                    {
                        if (objectProperty.GetGetMethod() is null)
                        {
                            continue;
                        }

                        try
                        {
                            string thisMacro = "{" + $"{parameter.Name}.{objectProperty.Name}" + "}";

                            if (TemplateValue != null && TemplateValue.Contains(thisMacro))
                            {
                                string replacement = objectProperty.GetValue(parameter.Value)?.ToString();

                                TemplateValue = TemplateValue.Replace(thisMacro, replacement);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }

            return TemplateValue;
        }
    }
}