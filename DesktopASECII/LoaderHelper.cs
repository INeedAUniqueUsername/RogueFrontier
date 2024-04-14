using Newtonsoft.Json;
using SadConsole;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using SadRogue.Primitives;
using System.IO;

namespace Common {
    //https://stackoverflow.com/a/57319194
    public static class STypeConverter {
        public static void PrepareConvert() {
            //https://stackoverflow.com/a/57319194
            TypeDescriptor.AddAttributes(typeof((int, int)),  new TypeConverterAttribute(typeof(Int2Converter)));
            
            TypeDescriptor.AddAttributes(typeof((uint, uint)), new TypeConverterAttribute(typeof(UInt2Converter)));
            //TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorConverter)));
        }
    }
    public class ColorConverter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var elements = Convert.ToString(value).Trim('(', ')').Split(',', StringSplitOptions.RemoveEmptyEntries);
            return new Color(int.Parse(elements[0]), int.Parse(elements[1]), int.Parse(elements[2]), int.Parse(elements[3]));
        }
    }
    public class Int2Converter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var elements = Convert.ToString(value).Trim('(').Trim(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return (int.Parse(elements.First()), int.Parse(elements.Last()));
        }
    }
    public class UInt2Converter : TypeConverter {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var elements = Convert.ToString(value).Trim('(').Trim(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return (uint.Parse(elements.First()), uint.Parse(elements.Last()));
        }
    }
}
