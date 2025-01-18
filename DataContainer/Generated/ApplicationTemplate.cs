using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DataContainer;

namespace DataContainer.Generated
{
    public partial class ApplicationTemplate : TemplateBase
    {
        public string NeedFocus { get; set; }
        public string HandleName { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        public bool UseMonitorDPI { get; set; }
    }
}