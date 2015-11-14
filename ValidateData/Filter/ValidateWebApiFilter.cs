using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace ValidateData.Filter
{
    public class ValidateWebApiFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var message = new HttpResponseMessage()
            {
                Content = new StringContent("One or more parameter values are invalid"),
                StatusCode = HttpStatusCode.BadRequest
            };

            if (!actionContext.ModelState.IsValid)
            {
                actionContext.Response = message;
            }

            foreach (var item in actionContext.ActionArguments)
            {
                if (!IsValid(item))
                {
                    actionContext.Response = message;
                }
            }
        }

        /// <summary>
        /// Checks the values of a class's properties to make sure they have valid values.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool IsValid(KeyValuePair<string, object> item)
        {
            Type myType = item.Value.GetType();

            // This currently does not check for every type. 
            if (myType == typeof(int))
            {
                // Check it nullable and null
                if (IsNullable(item.Value, item.Key) && item.Value != null)
                    if (!ValidateInt(item.Value)) return false;
            }
            if (myType == typeof(double))
            {
                // Check it nullable and null
                if (IsNullable(item.Value, item.Key) && item.Value != null)
                    if (!ValidateDouble(item.Value)) return false;
            }
            else if (myType == typeof(string))
            {
                if (item.Value != null)
                    if (!ValidateString(item.Value)) return false;
            }
            else if (myType.Namespace.Contains("System.Collections.Generic"))
            {
                if (item.Value != null)
                    if (!ValidateCollection(item.Value)) return false;
            }
            else if (myType.IsClass)
            {
                if (!ValidateClass(item.Value)) return false;
            }

            return true;
        }

        // Still work in progress. Needs tested. Better way of doing this?
        /// <summary>
        /// Checks if type is a nullable type for strongly typed variables.
        /// </summary>
        /// <param name="value">Property Value</param>
        /// <param name="key">Property Name </param>
        /// <returns>bool</returns>
        private bool IsNullable(object value, string key)
        {
            // Checking for nullable types:
            // https://msdn.microsoft.com/en-us/library/system.nullable.getunderlyingtype(v=vs.110).aspx

            Type myType = value.GetType();
            MethodInfo mi = myType.GetMethod(key);
            Type retval = mi.ReturnType;

            if (!retval.Namespace.Contains("Nullable")) return false;

            return true;
        }

        /// <summary>
        /// Iterates through the class's properties to see if they have valid values.
        /// </summary>
        /// <param name="value">Object of a class</param>
        /// <returns></returns>
        private bool ValidateClass(object value)
        {
            Type myType = value.GetType();
            IList<PropertyInfo> properties = new List<PropertyInfo>(myType.GetProperties());

            // You can even add custom checks for custom classes in here as needed.

            foreach (var property in properties)
            {
                if (!IsValid(new KeyValuePair<string, object>("Object", property))) return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a variable of type "Double" has a value of type double.
        /// </summary>
        /// <param name="value">Object of type double</param>
        /// <returns></returns>
        private bool ValidateDouble(object value)
        {
            try
            {
                double number;

                if (!double.TryParse(value.ToString(), out number)) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a variable of type "Integer" has a value of type integer.
        /// </summary>
        /// <param name="value">Object of type integer</param>
        /// <returns></returns>
        private bool ValidateInt(object value)
        {
            try
            {
                int number;

                if (!int.TryParse(value.ToString(), out number)) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a variable of type "String" has a value of type string.
        /// </summary>
        /// <param name="value">Object of type string</param>
        /// <returns></returns>
        private bool ValidateString(object value)
        {
            try
            {
                string val = (string)value;

                // This section can be modified as needed. This is a really strict
                // test currently. This is meant to help prevent scripting attacks.
                if (val != null)
                {
                    if (val.Contains('<')) return false;
                    if (val.Contains('>')) return false;
                    if (val.Contains(';')) return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Iterates through a collection of <T>. Finds the type of <T> and checks the values.
        /// </summary>
        /// <param name="value">Object of a colletion of <T> objects</param>
        /// <returns></returns>
        private bool ValidateCollection(object value)
        {
            try
            {
                ICollection valueList = value as ICollection;

                if (value != null)
                {
                    foreach (var valueItem in valueList)
                    {
                        var valueType = valueItem.GetType();

                        if (!IsValid(new KeyValuePair<string, object>("Object", valueItem))) return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
