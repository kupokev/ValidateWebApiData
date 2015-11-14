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
    public class ValidateFilter : ActionFilterAttribute
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
                if (!ValidateInt(item.Value)) return false;
            }
            if (myType == typeof(double))
            {
                if (!ValidateDouble(item.Value)) return false;
            }
            else if (myType == typeof(string))
            {
                if (!ValidateString(item.Value)) return false;
            }
            else if (myType.Namespace.Contains("System.Collections.Generic"))
            {
                if (!ValidateCollection(item.Value)) return false;
            }
            else if (myType.IsClass)
            {
                if (!ValidateClass(item.Value)) return false;
            }

            return true;
        }

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

        private bool ValidateString(object value)
        {
            try
            {
                string val = (string)value;

                // This section can be modified as needed. This is a really strict
                // test currently. This is meant to help prevent scripting attacks.
                if (val.Contains('<')) return false;
                if (val.Contains('>')) return false;
                if (val.Contains(';')) return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool ValidateCollection(object value)
        {
            try
            {
                ICollection valueList = value as ICollection;

                foreach (var valueItem in valueList)
                {
                    var valueType = valueItem.GetType();

                    if (!IsValid(new KeyValuePair<string, object>("Object", valueItem))) return false;
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
