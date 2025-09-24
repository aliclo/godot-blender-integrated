
using System.Collections.Generic;
using System.Linq;
using GdUnit4;
using Godot.Collections;

[TestSuite]
public class ObjectCopierTests
{

    [TestCase]
    [RequireGodotRuntime]
    public void CopyWithoutExcludeNorInclude_SameAsAltered()
    {
        // Arange
        var objectCopier = new NodeCopier();

        var original = new MyNestedNumbers()
        {
            Value = 1,
            Nested = new MyNestedNumbers()
            {
                Value = 2,
                Nested = new MyNestedNumbers()
                {
                    Value = 3
                }
            }
        };

        var altered = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 5,
                Nested = new MyNestedNumbers()
                {
                    Value = 6
                }
            }
        };

        // Act
        var result = objectCopier.CopyValues(original, altered, new Array<Array<string>>(), new Array<Array<string>>());

        // Assert
        var equalityHelper = new EqualityHelper();

        var expected = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 5,
                Nested = new MyNestedNumbers()
                {
                    Value = 6
                }
            }
        };

        equalityHelper.AssertEquals(expected, result);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CopyWithExclude_AlteredButExcludedSameAsOriginal()
    {
        // Arange
        var objectCopier = new NodeCopier();

        var original = new MyNestedNumbers()
        {
            Value = 1,
            Nested = new MyNestedNumbers()
            {
                Value = 2,
                Nested = new MyNestedNumbers()
                {
                    Value = 3
                }
            }
        };

        var altered = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 5,
                Nested = new MyNestedNumbers()
                {
                    Value = 6
                }
            }
        };

        // Act
        var result = objectCopier.CopyValues(original, altered, new Array<Array<string>>() { new Array<string>() { "Nested" } }, new Array<Array<string>>());

        // Assert
        var equalityHelper = new EqualityHelper();

        var expected = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 2,
                Nested = new MyNestedNumbers()
                {
                    Value = 3
                }
            }
        };

        equalityHelper.AssertEquals(expected, result);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CopyWithExcludeAndInclude_AlteredButExcludedSameAsOriginalAndIncludedSameAsAltered()
    {
        // Arange
        var objectCopier = new NodeCopier();

        var original = new MyNestedNumbers()
        {
            Value = 1,
            Nested = new MyNestedNumbers()
            {
                Value = 2,
                Nested = new MyNestedNumbers()
                {
                    Value = 3
                }
            }
        };

        var altered = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 5,
                Nested = new MyNestedNumbers()
                {
                    Value = 6
                }
            }
        };

        // Act
        var result = objectCopier.CopyValues(original, altered, new Array<Array<string>>() { new Array<string> { "Nested" } }, new Array<Array<string>>() { new Array<string> { "Nested", "Nested" } });

        // Assert
        var equalityHelper = new EqualityHelper();

        var expected = new MyNestedNumbers()
        {
            Value = 4,
            Nested = new MyNestedNumbers()
            {
                Value = 2,
                Nested = new MyNestedNumbers()
                {
                    Value = 6
                }
            }
        };

        equalityHelper.AssertEquals(expected, result);
    }

}