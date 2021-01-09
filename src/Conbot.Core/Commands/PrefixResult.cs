using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands
{
    public class PrefixResult : IResult
    {
        public string? Output { get; set; }
        public bool IsSuccessful => Output != null;
        public static PrefixResult Unsuccessful => new();

        public static PrefixResult Successful(string output) => new() { Output = output };

        public static implicit operator ValueTask<PrefixResult>(PrefixResult result)
            => new(result);
    }
}