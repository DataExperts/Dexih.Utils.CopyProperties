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
        public CopyPropertiesSimpleTypeException(Type type) : base(ModifyMessage(type))
        {
        }

        private static string ModifyMessage(Type type)
        {
            return $"The type {type.Name} is not supported by the CopyProperties extension as it is a simple type.  Use \"var newValue = value\" to copy simple variable type.";
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

    public class CopyPropertiesTargetInstanceException : CopyPropertiesException
    {
        public CopyPropertiesTargetInstanceException() : base(ModifyMessage())
        {
        }

        private static string ModifyMessage()
        {
            return $"The CopyProperties could not be created as a new instance of the target needs to be created.  Either, ensure the target object has been initialize and it is an array is the same size, or use the CopyProperties(this object source, ref object target) method or the CloneProperties(this object source) method.";
        }
    }
}
