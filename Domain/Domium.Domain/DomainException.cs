
namespace Domium.Domain
{
    public class DomainException : ApplicationException
    {
        public DomainException(string message) : base(message)
        {

        }
        public DomainException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
