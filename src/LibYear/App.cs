using LibYear.Core;
using Spectre.Console;

namespace LibYear;

public class App
{
	private readonly IPackageVersionChecker _checker;
	private readonly IProjectFileManager _projectFileManager;
	private readonly IAnsiConsole _console;
	private readonly IOutputFormatter _formatter;

	public App(IPackageVersionChecker checker, IProjectFileManager projectFileManager, IAnsiConsole console, IOutputFormatter formatter)
	{
		_checker = checker;
		_projectFileManager = projectFileManager;
		_console = console;
		_formatter = formatter;
	}

	public async Task<int> Run(Settings settings)
	{
		if (!settings.Json)
			_console.WriteLine();
		var projects = await _projectFileManager.GetAllProjects(settings.Paths, settings.Recursive);
		if (projects.Count == 0)
		{
			_console.WriteLine("No project files found");
			return 1;
		}

		var result = await _checker.GetPackages(projects);
		_formatter.Write(result);

		if (settings.Update)
		{
			var updated = await _projectFileManager.Update(result);
			foreach (var projectFile in updated)
			{
				_console.WriteLine($"{projectFile} updated");
			}
		}

		var limitChecker = new LimitChecker(settings);
		return limitChecker.AnyLimitsExceeded(result)
			? 1
			: 0;
	}

}