using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain;

namespace CJDSL.Application.Mapping;

/// <summary>
/// AutoMapper 映射配置
/// </summary>
public class DslMappingProfile : AutoMapper.Profile
{
    public DslMappingProfile()
    {
        // M1 对象 -> DSL 生成基础映射
        CreateMap<M1_Object, DslPage>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid().ToString("N")))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Components, opt => opt.Ignore());

        CreateMap<M1_Property, DslComponent>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => MapPropertyTypeToComponentType(src.Type)))
            .ForMember(dest => dest.Label, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.FieldName, opt => opt.MapFrom(src => src.Code))
            .ForMember(dest => dest.DataBind, opt => opt.MapFrom(src => $"@data.{src.Code}"))
            .ForMember(dest => dest.ValidationRules, opt => opt.MapFrom(src => MapValidationRules(src)))
            .ForMember(dest => dest.DataSource, opt => opt.MapFrom(src => MapDataSource(src)));
    }

    private static string MapPropertyTypeToComponentType(string type) => type.ToLower() switch
    {
        "string" => "text",
        "number" => "number",
        "date" => "date",
        "datetime" => "datetime",
        "select" => "select",
        "textarea" => "textarea",
        "boolean" => "switch",
        _ => "text"
    };

    private static List<DslValidationRule>? MapValidationRules(M1_Property prop)
    {
        var rules = new List<DslValidationRule>();
        if (prop.Required)
            rules.Add(new DslValidationRule { Type = "required", Message = $"{prop.Name}必填" });
        if (prop.MinLength.HasValue)
            rules.Add(new DslValidationRule { Type = "minLength", MinLength = prop.MinLength, Message = $"{prop.Name}长度不能小于{prop.MinLength}" });
        if (prop.MaxLength.HasValue)
            rules.Add(new DslValidationRule { Type = "maxLength", MaxLength = prop.MaxLength, Message = $"{prop.Name}长度不能大于{prop.MaxLength}" });
        if (!string.IsNullOrEmpty(prop.Pattern))
            rules.Add(new DslValidationRule { Type = "regex", Pattern = prop.Pattern, Message = $"{prop.Name}格式不正确" });
        return rules.Count > 0 ? rules : null;
    }

    private static DslDataSource? MapDataSource(M1_Property prop)
    {
        if (!string.IsNullOrEmpty(prop.DictCode))
        {
            return new DslDataSource
            {
                Type = "dictionary",
                Code = prop.DictCode
            };
        }
        return null;
    }
}
