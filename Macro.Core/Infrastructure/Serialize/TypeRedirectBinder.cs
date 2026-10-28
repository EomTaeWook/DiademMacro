using Macro.Models;
using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace Macro.Infrastructure.Serialize
{
    internal class TypeRedirectBinder : SerializationBinder
    {
        public override Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName + ", " + assemblyName, ResolveAssembly, ResolveType, true);
        }

        private static Assembly ResolveAssembly(AssemblyName assemblyName)
        {
            if (assemblyName.Name == "Macro")
            {
                return typeof(EventTriggerModel).Assembly;
            }
            if (assemblyName.Name == "WindowsBase")
            {
                return typeof(PointModel).Assembly;
            }
            if (assemblyName.Name == "System.Drawing")
            {
                return typeof(System.Drawing.Bitmap).Assembly;
            }
            return Assembly.Load(assemblyName);
        }

        private static Type ResolveType(Assembly assembly, string typeName, bool ignoreCase)
        {
            if (typeName == "Macro.Models.MouseTriggerInfo")
            {
                return typeof(MouseEventInfo);
            }
            if (typeName == "System.Windows.Point")
            {
                return typeof(PointModel);
            }
            if (typeName == "System.Drawing.Point")
            {
                return typeof(System.Drawing.Point);
            }
            if (assembly == null)
            {
                return Type.GetType(typeName, false, ignoreCase);
            }
            return assembly.GetType(typeName, false, ignoreCase);
        }
    }
}
