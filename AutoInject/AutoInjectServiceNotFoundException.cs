using ToolBX.AutoInject.Resources;

namespace ToolBX.AutoInject;

public class AutoInjectServiceNotFoundException : Exception
{
    public AutoInjectServiceNotFoundException(MemberInfo serviceType, MemberInfo implementationType) : base(string.Format(Exceptions.CannotFindService, serviceType.Name, implementationType.Name))
    {

    }
}