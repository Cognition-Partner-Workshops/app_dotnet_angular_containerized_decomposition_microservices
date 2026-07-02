using Xunit.Abstractions;
using Xunit.Sdk;

namespace Integration.Tests;

[AttributeUsage(AttributeTargets.Method)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        var sorted = new SortedDictionary<int, List<TTestCase>>();

        foreach (var testCase in testCases)
        {
            var priority = testCase.TestMethod.Method
                .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
                .FirstOrDefault()
                ?.GetNamedArgument<int>(nameof(TestPriorityAttribute.Priority)) ?? int.MaxValue;

            if (!sorted.TryGetValue(priority, out var list))
            {
                list = new List<TTestCase>();
                sorted[priority] = list;
            }

            list.Add(testCase);
        }

        foreach (var list in sorted.Values)
            foreach (var testCase in list)
                yield return testCase;
    }
}
