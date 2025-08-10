// Copyright (c) 2025 Christian Flessa. All rights reserved.
// This file is licensed under the MIT license. See LICENSE in the project root for more information.
namespace Chaos.BlazorMessageBus.Filtering;

using FluentAssertions;
using NUnit.Framework;

public class BlazorMessageBridgeFiltersTests
{
    [Test]
    public void All_ShouldAllowAll()
    {
        var filter = BlazorMessageBridgeFilters.All();
        filter.ShouldBridge(typeof(String)).Should().BeTrue();
        filter.ShouldBridge(typeof(Int32)).Should().BeTrue();
    }

    [Test]
    public void Exclude_Derived_NotExcluded_WhenBaseExcluded()
    {
        var filter = BlazorMessageBridgeFilters.Exclude(typeof(Base));
        filter.ShouldBridge(typeof(Base)).Should().BeFalse();
        filter.ShouldBridge(typeof(Derived)).Should().BeTrue();
    }

    [Test]
    public void Exclude_Empty_ShouldAllowAll()
    {
        var filter = BlazorMessageBridgeFilters.Exclude();
        filter.ShouldBridge(typeof(String)).Should().BeTrue();
        filter.ShouldBridge(typeof(Int32)).Should().BeTrue();
    }

    [Test]
    public void Exclude_NullArray_ShouldThrow()
    {
        FluentActions.Invoking(() => BlazorMessageBridgeFilters.Exclude(null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Include_Derived_NotMatched_WhenBaseIncluded()
    {
        var filter = BlazorMessageBridgeFilters.Include(typeof(Base));
        filter.ShouldBridge(typeof(Base)).Should().BeTrue();
        filter.ShouldBridge(typeof(Derived)).Should().BeFalse();
    }

    [Test]
    public void Include_Empty_ShouldBlockAll()
    {
        var filter = BlazorMessageBridgeFilters.Include();
        filter.ShouldBridge(typeof(String)).Should().BeFalse();
        filter.ShouldBridge(typeof(Int32)).Should().BeFalse();
    }

    [Test]
    public void Include_NullArray_ShouldThrow()
    {
        FluentActions.Invoking(() => BlazorMessageBridgeFilters.Include(null!))
                     .Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void Where_NullPredicate_ShouldThrow()
    {
        FluentActions.Invoking(() => BlazorMessageBridgeFilters.Where(null!))
                     .Should().Throw<ArgumentNullException>();
    }

    private class Base;

    private class Derived : Base;
}
