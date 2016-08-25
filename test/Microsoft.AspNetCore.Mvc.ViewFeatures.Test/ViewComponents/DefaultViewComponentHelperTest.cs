// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    public class DefaultViewComponentHelperTest
    {
        [Fact]
        public void GetArgumentDictionary_SupportsAnonymouslyTypedArguments()
        {
            // Arrange
            var descriptorCollectionProvider = Mock.Of<IViewComponentDescriptorCollectionProvider>();
            var selector = Mock.Of<IViewComponentSelector>();
            var invokerFactory = Mock.Of<IViewComponentInvokerFactory>();
            var viewBufferScope = Mock.Of<IViewBufferScope>();

            var helper = new DefaultViewComponentHelper(
                descriptorCollectionProvider,
                new HtmlTestEncoder(),
                selector,
                invokerFactory,
                viewBufferScope);

            var descriptor = CreateDescriptorForType(typeof(TestViewComponent));

            // Act
            var argumentDictionary = helper.GetArgumentDictionary(descriptor, new { a = 0 });

            // Assert
            Assert.Equal(1, argumentDictionary.Count);
            Assert.Collection(argumentDictionary,
                item =>
                {
                    Assert.Equal("a", item.Key);
                    Assert.IsType(typeof(int), item.Value);
                    Assert.Equal(0, item.Value);
                });
        }

        [Fact]
        public void GetArgumentDictionary_SingleParameter_DoesNotNeedAnonymouslyTypedArguments()
        {
            // Arrange
            var descriptorCollectionProvider = Mock.Of<IViewComponentDescriptorCollectionProvider>();
            var selector = Mock.Of<IViewComponentSelector>();
            var invokerFactory = Mock.Of<IViewComponentInvokerFactory>();
            var viewBufferScope = Mock.Of<IViewBufferScope>();

            var helper = new DefaultViewComponentHelper(
                descriptorCollectionProvider,
                new HtmlTestEncoder(),
                selector,
                invokerFactory,
                viewBufferScope);

            var descriptor = CreateDescriptorForType(typeof(TestViewComponent));

            // Act
            var argumentDictionary = helper.GetArgumentDictionary(descriptor, 0);

            // Assert
            Assert.Equal(1, argumentDictionary.Count);
            Assert.Collection(argumentDictionary,
                item =>
                {
                    Assert.Equal("a", item.Key);
                    Assert.IsType(typeof(int), item.Value);
                    Assert.Equal(0, item.Value);
                });
        }

        private ViewComponentDescriptor CreateDescriptorForType(Type componentType)
        {
            var provider = CreateProvider(componentType);
            return provider.GetViewComponents().First();
        }

        private class TestViewComponent
        {
            public IViewComponentResult Invoke(int a) => null;
        }

        private DefaultViewComponentDescriptorProvider CreateProvider(Type componentType)
        {
            return new FilteredViewComponentDescriptorProvider(componentType);
        }

        // This will only consider types nested inside this class as ViewComponent classes
        private class FilteredViewComponentDescriptorProvider : DefaultViewComponentDescriptorProvider
        {
            public FilteredViewComponentDescriptorProvider(params Type[] allowedTypes)
                : base(GetApplicationPartManager(allowedTypes.Select(t => t.GetTypeInfo())))
            {
            }

            private static ApplicationPartManager GetApplicationPartManager(IEnumerable<TypeInfo> types)
            {
                var manager = new ApplicationPartManager();
                manager.ApplicationParts.Add(new TestApplicationPart(types));
                manager.FeatureProviders.Add(new TestFeatureProvider());
                return manager;
            }

            private class TestFeatureProvider : IApplicationFeatureProvider<ViewComponentFeature>
            {
                public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
                {
                    foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
                    {
                        feature.ViewComponents.Add(type);
                    }
                }
            }
        }
    }
}
