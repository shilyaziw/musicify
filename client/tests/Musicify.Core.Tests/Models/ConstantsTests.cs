using FluentAssertions;
using Musicify.Core.Models.Constants;
using Xunit;

namespace Musicify.Core.Tests.Models;

/// <summary>
/// 常量类测试
/// 验证歌曲类型、风格、语言等常量的有效性检查
/// </summary>
public class ConstantsTests
{
    #region SongTypes Tests
    
    [Theory]
    [InlineData("流行", true)]
    [InlineData("摇滚", true)]
    [InlineData("说唱", true)]
    [InlineData("民谣", true)]
    [InlineData("电子", true)]
    [InlineData("古风", true)]
    [InlineData("R&B", true)]
    [InlineData("爵士", true)]
    [InlineData("乡村", true)]
    [InlineData("金属", true)]
    [InlineData("无效类型", false)]
    [InlineData("", false)]
    public void SongTypes_IsValid_ShouldReturnCorrectResult(string type, bool expected)
    {
        // Act & Assert
        SongTypes.IsValid(type).Should().Be(expected);
    }
    
    [Fact]
    public void SongTypes_All_ShouldContainAllTypes()
    {
        // Act
        var all = SongTypes.All;
        
        // Assert
        all.Should().HaveCount(10);
        all.Should().Contain("流行");
        all.Should().Contain("古风");
        all.Should().Contain("金属");
    }
    
    #endregion
    
    #region Styles Tests
    
    [Theory]
    [InlineData("抒情", true)]
    [InlineData("激昂", true)]
    [InlineData("轻快", true)]
    [InlineData("忧郁", true)]
    [InlineData("治愈", true)]
    [InlineData("燃爆", true)]
    [InlineData("平静", true)]
    [InlineData("梦幻", true)]
    [InlineData("无效风格", false)]
    public void Styles_IsValid_ShouldReturnCorrectResult(string style, bool expected)
    {
        // Act & Assert
        Styles.IsValid(style).Should().Be(expected);
    }
    
    [Fact]
    public void Styles_All_ShouldContainAllStyles()
    {
        // Act
        var all = Styles.All;
        
        // Assert
        all.Should().HaveCount(8);
        all.Should().Contain("抒情");
        all.Should().Contain("梦幻");
    }
    
    #endregion
    
    #region Languages Tests
    
    [Theory]
    [InlineData("中文", true)]
    [InlineData("英文", true)]
    [InlineData("粤语", true)]
    [InlineData("日语", true)]
    [InlineData("韩语", true)]
    [InlineData("中英混合", true)]
    [InlineData("其他", true)]
    [InlineData("火星语", false)]
    public void Languages_IsValid_ShouldReturnCorrectResult(string language, bool expected)
    {
        // Act & Assert
        Languages.IsValid(language).Should().Be(expected);
    }
    
    [Fact]
    public void Languages_All_ShouldContainAllLanguages()
    {
        // Act
        var all = Languages.All;
        
        // Assert
        all.Should().HaveCount(7);
        all.Should().Contain("中文");
        all.Should().Contain("粤语");
    }
    
    #endregion
    
    #region Platforms Tests
    
    [Fact]
    public void Platforms_All_ShouldContainAllPlatforms()
    {
        // Act
        var all = Platforms.All;
        
        // Assert
        all.Should().HaveCount(9);
        all.Should().Contain("QQ音乐");
        all.Should().Contain("抖音");
        all.Should().Contain("Spotify");
    }
    
    #endregion
    
    #region CreationModes Tests
    
    [Theory]
    [InlineData("coach", true)]
    [InlineData("express", true)]
    [InlineData("hybrid", true)]
    [InlineData("invalid", false)]
    public void CreationModes_IsValid_ShouldReturnCorrectResult(string mode, bool expected)
    {
        // Act & Assert
        CreationModes.IsValid(mode).Should().Be(expected);
    }
    
    #endregion
}
