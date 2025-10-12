using System;
using System.Runtime.Serialization;

namespace Macro.Infrastructure.Serialize
{
    internal class TypeRedirectBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            if (typeName == "Macro.Models.MouseTriggerInfo")
            {
                return typeof(Macro.Models.MouseEventInfo);
            }
            return Type.GetType($"{typeName}, {assemblyName}");
        }
    }
}
