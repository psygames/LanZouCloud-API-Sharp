using LitJson;

namespace LanZou
{
    /// <summary>
    /// 重写ToString，以JSON格式输出
    /// </summary>
    public class JsonStringObject
    {
        public override string ToString()
        {
            return JsonMapper.ToJson(this);
        }
    }
}
