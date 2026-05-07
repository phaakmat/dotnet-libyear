using LibYear.Core;

namespace LibYear;

public interface IOutputFormatter
{
	void Write(SolutionResult result);
}
