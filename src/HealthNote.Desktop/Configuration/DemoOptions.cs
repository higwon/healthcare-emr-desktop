using System;

namespace HealthNote.Desktop.Configuration
{
    public sealed class DemoOptions
    {
        private DemoOptions(Uri apiBaseUri, bool smoke)
        {
            ApiBaseUri = apiBaseUri;
            Smoke = smoke;
        }

        public Uri ApiBaseUri { get; }
        public bool Smoke { get; }

        public static DemoOptions Parse(string[] arguments)
        {
            string address = "http://127.0.0.1:5078/";
            bool smoke = false;
            bool addressSpecified = false;
            for (int index = 0; index < arguments.Length; index++)
            {
                switch (arguments[index])
                {
                    case "--smoke":
                        smoke = true;
                        break;
                    case "--api-base-url":
                        if (addressSpecified || ++index >= arguments.Length)
                        {
                            throw new ArgumentException("Invalid demo options.");
                        }

                        addressSpecified = true;
                        address = arguments[index];
                        break;
                    default:
                        throw new ArgumentException("Invalid demo options.");
                }
            }

            if (!Uri.TryCreate(address, UriKind.Absolute, out Uri? uri) ||
                uri.Scheme != Uri.UriSchemeHttp || uri.Host != "127.0.0.1" || uri.Port <= 0 ||
                uri.AbsolutePath != "/" || uri.UserInfo.Length != 0 ||
                uri.Query.Length != 0 || uri.Fragment.Length != 0)
            {
                throw new ArgumentException("Invalid demo API address.");
            }

            return new DemoOptions(uri, smoke);
        }
    }
}
