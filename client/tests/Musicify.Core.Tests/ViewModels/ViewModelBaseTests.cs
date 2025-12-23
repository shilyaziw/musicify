using Xunit;
using FluentAssertions;
using Musicify.Core.ViewModels;
using System.ComponentModel;

namespace Musicify.Core.Tests.ViewModels;

/// <summary>
/// ViewModelBase 基类测试
/// </summary>
public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _testProperty = string.Empty;
        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }

        private int _counter;
        public int Counter
        {
            get => _counter;
            set => SetProperty(ref _counter, value);
        }
    }

    [Fact]
    public void Constructor_ShouldInitializeSuccessfully()
    {
        // Act
        var vm = new TestViewModel();

        // Assert
        vm.Should().NotBeNull();
        vm.TestProperty.Should().BeEmpty();
    }

    [Fact]
    public void SetProperty_WhenValueChanges_ShouldRaisePropertyChanged()
    {
        // Arrange
        var vm = new TestViewModel();
        var eventRaised = false;
        var propertyName = string.Empty;

        vm.PropertyChanged += (sender, e) =>
        {
            eventRaised = true;
            propertyName = e.PropertyName;
        };

        // Act
        vm.TestProperty = "New Value";

        // Assert
        eventRaised.Should().BeTrue();
        propertyName.Should().Be(nameof(vm.TestProperty));
        vm.TestProperty.Should().Be("New Value");
    }

    [Fact]
    public void SetProperty_WhenValueNotChanged_ShouldNotRaisePropertyChanged()
    {
        // Arrange
        var vm = new TestViewModel { TestProperty = "Initial" };
        var eventRaised = false;

        vm.PropertyChanged += (sender, e) => eventRaised = true;

        // Act
        vm.TestProperty = "Initial";

        // Assert
        eventRaised.Should().BeFalse();
    }

    [Fact]
    public void SetProperty_WithValueType_ShouldWorkCorrectly()
    {
        // Arrange
        var vm = new TestViewModel();
        var eventRaised = false;

        vm.PropertyChanged += (sender, e) => eventRaised = true;

        // Act
        vm.Counter = 42;

        // Assert
        eventRaised.Should().BeTrue();
        vm.Counter.Should().Be(42);
    }

    [Fact]
    public void INotifyPropertyChanged_ShouldBeImplemented()
    {
        // Arrange & Act
        var vm = new TestViewModel();

        // Assert
        vm.Should().BeAssignableTo<INotifyPropertyChanged>();
    }
}
