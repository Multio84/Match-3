
// an interface to control order of initialization by GameBootstrapper
// executed only once on child object initialization
public interface IInitializer
{
    public void Init();
}
