using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using TileTuple = (uint Foreground, uint Background, int Glyph);
namespace ASECII;
    //https://stackoverflow.com/a/57319194
    public static class STypeConverter {
        public static void PrepareConvert() {
            //https://stackoverflow.com/a/57319194
            TypeDescriptor.AddAttributes(typeof((int, int)),  new TypeConverterAttribute(typeof(Int2Converter)));
			TypeDescriptor.AddAttributes(typeof((uint, uint)), new TypeConverterAttribute(typeof(UInt2Converter)));
			TypeDescriptor.AddAttributes(typeof(TileTuple), new TypeConverterAttribute(typeof(TileTupleConverter)));
			//TypeDescriptor.AddAttributes(typeof(Color), new TypeConverterAttribute(typeof(ColorConverter)));
		}
    }
    public static class ASECIILoader {
        public static Dictionary<(int, int), TileValue> LoadCG(string path) =>
            DeserializeObject<Dictionary<(int, int), TileValue>>(File.ReadAllText(path));
        public static T DeserializeObject<T>(string s) {
            STypeConverter.PrepareConvert();
            return JsonConvert.DeserializeObject<T>(s, SFileMode.settings);
        }
        public static string SerializeObject(object o) {
            STypeConverter.PrepareConvert();
            return JsonConvert.SerializeObject(o, SFileMode.settings);
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
		public override bool CanConvertTo (ITypeDescriptorContext context, [NotNullWhen(true)] Type destinationType) {
			return destinationType == typeof(string) || base.CanConvertTo(context, destinationType);
		}
		public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
			var v = ((uint Front, uint Back, int Glyph)) value;
            return $"({v.Front}, {v.Back}, {v.Glyph})";
		}
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var elements = Convert.ToString(value).Trim('(').Trim(')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return (uint.Parse(elements.First()), uint.Parse(elements.Last()));
        }
    }
	public class TileTupleConverter : TypeConverter {
		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
			return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
		}
		public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
			var elements = Convert.ToString(value).Trim('(', ')').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToArray();
			return (uint.Parse(elements[0]), uint.Parse(elements[1]), int.Parse(elements[2]));
		}
	}