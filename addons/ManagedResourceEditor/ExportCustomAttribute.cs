using System;

namespace ManagedResourceEditor
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExportCustomAttribute: Attribute
    {
        
    }
}