﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Squidex.Domain.Apps.Core.Apps;
using Squidex.Infrastructure.Security;
using Xunit;

#pragma warning disable SA1310 // Field names must not contain underscore

namespace Squidex.Domain.Apps.Core.Model.Apps
{
    public class RolesTests
    {
        private readonly Roles roles_0;
        private readonly string firstRole = "Role1";
        private readonly string role = "Role2";

        public RolesTests()
        {
            roles_0 = Roles.Empty.Add(firstRole);
        }

        [Fact]
        public void Should_create_roles_without_defaults()
        {
            var roles = new Roles(new Dictionary<string, Role>(Roles.Defaults));

            Assert.Equal(0, roles.CustomCount);
        }

        [Fact]
        public void Should_add_role()
        {
            var roles_1 = roles_0.Add(role);

            roles_1[role].Should().BeEquivalentTo(new Role(role, PermissionSet.Empty));
        }

        [Fact]
        public void Should_throw_exception_if_add_role_with_same_name()
        {
            var roles_1 = roles_0.Add(role);

            Assert.Throws<ArgumentException>(() => roles_1.Add(role));
        }

        [Fact]
        public void Should_do_nothing_if_role_to_add_is_default()
        {
            var roles_1 = roles_0.Add(Role.Developer);

            Assert.True(roles_1.CustomCount > 0);
        }

        [Fact]
        public void Should_update_role()
        {
            var roles_1 = roles_0.Update(firstRole, "P1", "P2");

            roles_1[firstRole].Should().BeEquivalentTo(new Role(firstRole, new PermissionSet("P1", "P2")));
        }

        [Fact]
        public void Should_return_same_roles_if_role_is_updated_with_same_values()
        {
            var roles_1 = roles_0.Update(firstRole);

            Assert.Same(roles_0, roles_1);
        }

        [Fact]
        public void Should_return_same_roles_if_role_not_found()
        {
            var roles_1 = roles_0.Update(role, "P1", "P2");

            Assert.Same(roles_0, roles_1);
        }

        [Fact]
        public void Should_remove_role()
        {
            var roles_1 = roles_0.Add("role1");
            var roles_2 = roles_1.Add("role2");
            var roles_3 = roles_2.Remove(firstRole);

            Assert.Equal(new[] { "role1", "role2" }, roles_3.Custom.Select(x => x.Name));
        }

        [Fact]
        public void Should_do_nothing_if_remove_role_not_found()
        {
            var roles_1 = roles_0.Remove(role);

            Assert.Same(roles_0, roles_1);
        }

        [Fact]
        public void Should_get_custom_roles()
        {
            var names = roles_0.Custom.Select(x => x.Name).ToArray();

            Assert.Equal(new[] { firstRole }, names);
        }

        [Fact]
        public void Should_get_all_roles()
        {
            var names = roles_0.All.Select(x => x.Name).ToArray();

            Assert.Equal(new[] { firstRole, "Owner", "Reader", "Editor", "Developer" }, names);
        }

        [Fact]
        public void Should_check_for_custom_role()
        {
            Assert.True(roles_0.ContainsCustom(firstRole));
        }

        [Fact]
        public void Should_check_for_non_custom_role()
        {
            Assert.False(roles_0.ContainsCustom(Role.Owner));
        }

        [Fact]
        public void Should_check_for_default_role()
        {
            Assert.True(Roles.IsDefault(Role.Owner));
        }

        [Fact]
        public void Should_check_for_non_default_role()
        {
            Assert.False(Roles.IsDefault(firstRole));
        }

        [InlineData("Developer")]
        [InlineData("Editor")]
        [InlineData("Owner")]
        [InlineData("Reader")]
        [Theory]
        public void Should_get_default_roles(string name)
        {
            var found = roles_0.TryGet("app", name, out var result);

            Assert.True(found);
            Assert.True(result!.IsDefault);
            Assert.True(roles_0.Contains(name));

            foreach (var permission in result.Permissions)
            {
                Assert.StartsWith("squidex.apps.app.", permission.Id);
            }
        }

        [Fact]
        public void Should_return_null_if_role_not_found()
        {
            var found = roles_0.TryGet("app", "custom", out var result);

            Assert.False(found);
            Assert.Null(result);
        }
    }
}