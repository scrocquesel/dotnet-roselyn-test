namespace SpecificNamespace;

public class Class1
{
    public void Run()
    {
        // should call specificInterface.Process();
    }

}


public interface ISpecificInterface
{
    void Process();
}
