using GdUnit4;
using Godot.Collections;
using static GdUnit4.Assertions;

[TestSuite]
public class EqualityHelperTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void TwoSimpleEqualObjects_Null()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var anotherRedCar = new Car() { Colour = "Red" };

        // Act
        var result = equalityHelper.IsEqual(redCar, anotherRedCar);

        // Assert
        AssertString(result).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoSimpleDifferentObjects_False()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var blueCar = new Car() { Colour = "Blue" };

        // Act
        var result = equalityHelper.IsEqual(redCar, blueCar);

        // Assert
        AssertString(result).IsEqual(".Colour: Red != Blue");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoSimpleCompletelyDifferentObjects_False()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var hundredTrain = new Train() { TopSpeed = 100 };

        // Act
        var result = equalityHelper.IsEqual(redCar, hundredTrain);

        // Assert
        AssertString(result).IsEqual(": prop meta mismatch");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoSameArrays_True()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var anotherRedCar = new Car() { Colour = "Red" };

        var array = new Array([1, "2", redCar]);
        var sameArray = new Array([1, "2", anotherRedCar]);

        // Act
        var result = equalityHelper.IsEqual(array, sameArray);

        // Assert
        AssertString(result).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoDifferentArrays_False()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var blueCar = new Car() { Colour = "Blue" };

        var array = new Array([1, "2", redCar]);
        var differentArray = new Array([7, "a", blueCar]);

        // Act
        var result = equalityHelper.IsEqual(array, differentArray);

        // Assert
        AssertString(result).IsEqual("[0]: 1 != 7");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoSameDictionaries_True()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var anotherRedCar = new Car() { Colour = "Red" };
        var blueCar = new Car() { Colour = "Blue" };
        var anotherBlueCar = new Car() { Colour = "Blue" };

        var dictionary = new Dictionary();
        var sameDictionary = new Dictionary();

        dictionary[1] = 1;
        dictionary["test"] = "bla";
        dictionary[redCar] = blueCar;

        sameDictionary[1] = 1;
        sameDictionary["test"] = "bla";
        sameDictionary[anotherRedCar] = anotherBlueCar;

        // Act
        var result = equalityHelper.IsEqual(dictionary, sameDictionary);

        // Assert
        AssertString(result).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoDifferentDictionaries_False()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var anotherRedCar = new Car() { Colour = "Red" };
        var blueCar = new Car() { Colour = "Blue" };
        var greenCar = new Car() { Colour = "Green" };

        var dictionary = new Dictionary();
        var differentDictionary = new Dictionary();

        dictionary[1] = 1;
        dictionary["test"] = "bla";
        dictionary[redCar] = blueCar;

        differentDictionary[1] = 7;
        differentDictionary["test"] = "nah";
        differentDictionary[anotherRedCar] = greenCar;

        // Act
        var result = equalityHelper.IsEqual(dictionary, differentDictionary);

        // Assert
        AssertString(result).IsEqual("[1]: 1 != 7");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TwoDifferentKeyDictionaries_False()
    {
        // Arrange
        var equalityHelper = new EqualityHelper();

        var redCar = new Car() { Colour = "Red" };
        var anotherRedCar = new Car() { Colour = "Red" };
        var blueCar = new Car() { Colour = "Blue" };
        var anotherBlueCar = new Car() { Colour = "Blue" };

        var dictionary = new Dictionary();
        var differentKeyDictionary = new Dictionary();

        dictionary[1] = 1;
        dictionary["test"] = "bla";
        dictionary[redCar] = blueCar;

        differentKeyDictionary[7] = 1;
        differentKeyDictionary["nah"] = "bla";
        differentKeyDictionary[anotherBlueCar] = anotherRedCar;

        // Act
        var result = equalityHelper.IsEqual(dictionary, differentKeyDictionary);

        // Assert
        AssertString(result).IsEqual(": Dictionary keys mismatch");
    }
    
}