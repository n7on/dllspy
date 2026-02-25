using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DllSpy.Core.Helpers;
using Xunit;

namespace DllSpy.Core.Tests.Helpers
{
    public class ReflectionHelperTests
    {
        #region GetFriendlyTypeName

        [Fact]
        public void GetFriendlyTypeName_Void()
            => Assert.Equal("void", ReflectionHelper.GetFriendlyTypeName(typeof(void)));

        [Fact]
        public void GetFriendlyTypeName_String()
            => Assert.Equal("string", ReflectionHelper.GetFriendlyTypeName(typeof(string)));

        [Fact]
        public void GetFriendlyTypeName_Int()
            => Assert.Equal("int", ReflectionHelper.GetFriendlyTypeName(typeof(int)));

        [Fact]
        public void GetFriendlyTypeName_Bool()
            => Assert.Equal("bool", ReflectionHelper.GetFriendlyTypeName(typeof(bool)));

        [Fact]
        public void GetFriendlyTypeName_Task()
            => Assert.Equal("Task", ReflectionHelper.GetFriendlyTypeName(typeof(Task)));

        [Fact]
        public void GetFriendlyTypeName_TaskOfString()
            => Assert.Equal("Task<string>", ReflectionHelper.GetFriendlyTypeName(typeof(Task<string>)));

        [Fact]
        public void GetFriendlyTypeName_ListOfInt()
            => Assert.Equal("List<int>", ReflectionHelper.GetFriendlyTypeName(typeof(List<int>)));

        [Fact]
        public void GetFriendlyTypeName_IntArray()
            => Assert.Equal("int[]", ReflectionHelper.GetFriendlyTypeName(typeof(int[])));

        [Fact]
        public void GetFriendlyTypeName_DictionaryOfStringInt()
            => Assert.Equal("Dictionary<string, int>", ReflectionHelper.GetFriendlyTypeName(typeof(Dictionary<string, int>)));

        [Fact]
        public void GetFriendlyTypeName_NullableInt()
            => Assert.Equal("Nullable<int>", ReflectionHelper.GetFriendlyTypeName(typeof(int?)));

        #endregion

        #region IsAsyncMethod

        private class MethodSamples
        {
            public Task AsyncTask() => Task.CompletedTask;
            public Task<string> AsyncTaskOfT() => Task.FromResult("test");
            public ValueTask AsyncValueTask() => default;
            public ValueTask<int> AsyncValueTaskOfT() => default;
            public void SyncVoid() { }
            public string SyncString() => "test";
        }

        private static MethodInfo GetSampleMethod(string name)
            => typeof(MethodSamples).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        [Fact]
        public void IsAsyncMethod_Task_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsAsyncMethod(GetSampleMethod("AsyncTask")));

        [Fact]
        public void IsAsyncMethod_TaskOfT_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsAsyncMethod(GetSampleMethod("AsyncTaskOfT")));

        [Fact]
        public void IsAsyncMethod_ValueTask_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsAsyncMethod(GetSampleMethod("AsyncValueTask")));

        [Fact]
        public void IsAsyncMethod_ValueTaskOfT_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsAsyncMethod(GetSampleMethod("AsyncValueTaskOfT")));

        [Fact]
        public void IsAsyncMethod_SyncVoid_ReturnsFalse()
            => Assert.False(ReflectionHelper.IsAsyncMethod(GetSampleMethod("SyncVoid")));

        [Fact]
        public void IsAsyncMethod_SyncString_ReturnsFalse()
            => Assert.False(ReflectionHelper.IsAsyncMethod(GetSampleMethod("SyncString")));

        #endregion

        #region IsNullableType

        [Fact]
        public void IsNullableType_Int_ReturnsFalse()
            => Assert.False(ReflectionHelper.IsNullableType(typeof(int)));

        [Fact]
        public void IsNullableType_NullableInt_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsNullableType(typeof(int?)));

        [Fact]
        public void IsNullableType_String_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsNullableType(typeof(string)));

        [Fact]
        public void IsNullableType_Bool_ReturnsFalse()
            => Assert.False(ReflectionHelper.IsNullableType(typeof(bool)));

        [Fact]
        public void IsNullableType_Object_ReturnsTrue()
            => Assert.True(ReflectionHelper.IsNullableType(typeof(object)));

        #endregion
    }
}
