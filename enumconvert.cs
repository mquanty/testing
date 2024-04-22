using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Reflection;

public class EnumJsonConverter : StringEnumConverter
{
    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.String)
        {
            string enumText = reader.Value.ToString();
            Type enumType = Nullable.GetUnderlyingType(objectType) ?? objectType;
            return GetEnumValueFromDescription(enumType, enumText);
        }
        else
        {
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value != null && value is Enum)
        {
            Type enumType = value.GetType();
            writer.WriteValue(GetEnumDescription((Enum)value));
        }
        else
        {
            base.WriteJson(writer, value, serializer);
        }
    }

    private string GetEnumDescription(Enum value)
    {
        FieldInfo fieldInfo = value.GetType().GetField(value.ToString());
        DescriptionAttribute[] attributes = (DescriptionAttribute[])fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
        return attributes.Length > 0 ? attributes[0].Description : value.ToString();
    }

    private object GetEnumValueFromDescription(Type enumType, string description)
    {
        foreach (object enumValue in Enum.GetValues(enumType))
        {
            Enum value = (Enum)enumValue;
            if (GetEnumDescription(value) == description)
            {
                return enumValue;
            }
        }
        throw new ArgumentException($"No enum value with description '{description}' found in {enumType.Name}");
    }
}

// Global setting to use the EnumJsonConverter for all enums
JsonConvert.DefaultSettings = () => new JsonSerializerSettings
{
    Converters = { new EnumJsonConverter() }
};

// Usage example
public class MyClass
{
    public MyEnum EnumProperty { get; set; }
}

public enum MyEnum
{
    [Description("Primary Line")]
    PrimaryLine,
    
    [Description("Secondary Line")]
    SecondaryLine
}

class Program
{
    static void Main(string[] args)
    {
        MyClass obj = new MyClass { EnumProperty = MyEnum.PrimaryLine };
        string json = JsonConvert.SerializeObject(obj);
        Console.WriteLine(json); // Output: {"EnumProperty":"Primary Line"}

        MyClass deserializedObj = JsonConvert.DeserializeObject<MyClass>(json);
        Console.WriteLine(deserializedObj.EnumProperty); // Output: PrimaryLine
    }
}
