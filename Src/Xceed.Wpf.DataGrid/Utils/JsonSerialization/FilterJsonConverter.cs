using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xceed.Wpf.DataGrid.Utils.JsonSerialization
{
  public class FilterSpecifiedConcreteClassConverter : DefaultContractResolver
  {
    protected override JsonConverter ResolveContractConverter(Type objectType)
    {
      if (typeof(IFilter).IsAssignableFrom(objectType) && !objectType.IsAbstract)
        return null;
      return base.ResolveContractConverter(objectType);
    }
  }

  public class FilterJsonConverter : JsonConverter
  {
    static JsonSerializerSettings SpecifiedSubclassConversion = new JsonSerializerSettings() { ContractResolver = new FilterSpecifiedConcreteClassConverter() };

    public override bool CanConvert(Type objectType)
    {
      return (objectType == typeof(IFilter));
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
      JObject jo = JObject.Load(reader);
      switch(jo["FilterType"].Value<int>())
      {
        case 1:
          return JsonConvert.DeserializeObject<ListFilter>(jo.ToString(), SpecifiedSubclassConversion);
        case 2:
          return JsonConvert.DeserializeObject<TextFilter>(jo.ToString(), SpecifiedSubclassConversion);
        default:
          return new Exception();
      }

      throw new NotImplementedException();
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
      throw new NotImplementedException();
    }
  }
}
