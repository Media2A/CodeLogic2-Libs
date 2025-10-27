using System;
using System.Net;

namespace CL.Core.Utilities
{
    public partial class CLU_Web
    {
        /// <summary>
        /// Creates a new record by sending a POST request to the specified API URL.
        /// </summary>
        /// <param name="apiUrl">The URL of the API to create the record on.</param>
        /// <param name="data">The data to be sent as the request body (in JSON format).</param>
        /// <returns>The response from the API.</returns>
        public static string CreateRecord(string apiUrl, string data)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string response = client.UploadString(apiUrl, "POST", data);
                    return response;
                }
            }
            catch (WebException ex)
            {
                // Handle any exceptions here
                return ex.Message;
            }
        }

        /// <summary>
        /// Reads a record by sending a GET request to the specified API URL with the given ID.
        /// </summary>
        /// <param name="apiUrl">The URL of the API to read the record from.</param>
        /// <param name="id">The ID of the record to retrieve.</param>
        /// <returns>The response from the API.</returns>
        public static string ReadRecord(string apiUrl, int id)
        {
            try
            {
                using (var client = new WebClient())
                {
                    string response = client.DownloadString($"{apiUrl}/{id}");
                    return response;
                }
            }
            catch (WebException ex)
            {
                // Handle any exceptions here
                return ex.Message;
            }
        }

        /// <summary>
        /// Updates an existing record by sending a PUT request to the specified API URL with the given ID.
        /// </summary>
        /// <param name="apiUrl">The URL of the API to update the record on.</param>
        /// <param name="id">The ID of the record to update.</param>
        /// <param name="data">The data to be sent as the request body (in JSON format).</param>
        /// <returns>The response from the API.</returns>
        public static string UpdateRecord(string apiUrl, int id, string data)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string response = client.UploadString($"{apiUrl}/{id}", "PUT", data);
                    return response;
                }
            }
            catch (WebException ex)
            {
                // Handle any exceptions here
                return ex.Message;
            }
        }

        /// <summary>
        /// Deletes a record by sending a DELETE request to the specified API URL with the given ID.
        /// </summary>
        /// <param name="apiUrl">The URL of the API to delete the record from.</param>
        /// <param name="id">The ID of the record to delete.</param>
        /// <returns>The response from the API.</returns>
        public static string DeleteRecord(string apiUrl, int id)
        {
            try
            {
                using (var client = new WebClient())
                {
                    string response = client.UploadString($"{apiUrl}/{id}", "DELETE", string.Empty);
                    return response;
                }
            }
            catch (WebException ex)
            {
                // Handle any exceptions here
                return ex.Message;
            }
        }
    }
}
