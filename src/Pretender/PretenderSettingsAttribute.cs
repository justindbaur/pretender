using Pretender.Settings;

namespace Pretender
{

    [AttributeUsage(AttributeTargets.Assembly)]
    public class PretenderSettingsAttribute : Attribute
    {
        public PretendBehavior Behavior { get; set; }
    }
}
