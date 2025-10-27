using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        // Form data
        public static async Task<Dictionary<string, object>> GetFormDataAsync()
        {
            var context = CLU_Web.HC(); // Assuming this retrieves the current HttpContext

            // Ensure the content type is form-data (used for both fields and file uploads)
            if (context.Request.HasFormContentType)
            {
                var form = await context.Request.ReadFormAsync();
                var formData = new Dictionary<string, object>();

                // Extract form fields (key-value pairs)
                foreach (var field in form)
                {
                    formData.Add(field.Key, field.Value.ToString());
                }

                // Extract file data (key-value pair with file objects)
                foreach (var file in form.Files)
                {
                    formData.Add(file.Name, file); // Adding the file as the value in the dictionary
                }

                return formData;
            }

            return null; // Return null if the request is not form-data
        }
    }
}
