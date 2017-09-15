using System;
using System.Collections.Generic;
using System.Text;

namespace Dexih.Utils.CopyProperties
{
    public class CopyPropertiesException : Exception
    {
        public CopyPropertiesException()
        {
        }
        public CopyPropertiesException(string message) : base(message)
        {
        }
        public CopyPropertiesException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    public class CopyPropertiesSimpleTypeException : CopyPropertiesException
    {
        public CopyPropertiesSimpleTypeException(object value) : base(ModifyMessage(value))
        {
        }

        private static string ModifyMessage(object value)
        {
            return $"The value {value} is not supported by the CopyProperties extension as it is a simple type.  Use \"var newValue = value\" to copy simple variable type.";
        }
    }

    public class CopyPropertiesNullException : CopyPropertiesException
    {
        public CopyPropertiesNullException() : base(ModifyMessage())
        {
        }

        private static string ModifyMessage()
        {
            return $"Null values are not supported by the CopyProperties extension.  Ensure both the source and target objects are not null.";
        }
    }

    public class CopyPropertiesInvalidCollectionException : CopyPropertiesException
    {
        public CopyPropertiesInvalidCollectionException(string message) : base(message)
        {
        }
    }
}
