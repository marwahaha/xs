﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;
using System.Reflection;
using Windows.UI.Xaml.Controls.Primitives;

namespace XSRT2 {
	public static class Handler
	{
		internal static class FrameworkElementHandler
        {
            internal static void SetProperties(FrameworkElement t, JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap)
            {
                TrySet(obj, lastObj, "horizontalAlignment", t, (target, x, lastX) => target.HorizontalAlignment = ParseEnum<HorizontalAlignment>(x));
                TrySet(obj, lastObj, "verticalAlignment", t, (target, x, lastX) => target.VerticalAlignment = ParseEnum<VerticalAlignment>(x));
                TrySet(obj, lastObj, "margin", t, (target, x, lastX) => target.Margin = XamlStringParse<Thickness>(x));
                TrySet(obj, lastObj, "name", t, (target, x, lastX) => { target.Name = x.ToString(); namedObjectMap[target.Name] = target; });
            }
        }

		internal static class TextBlockHandler
        {
            internal static TextBlock Create(JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap)
            {
                var createResult = CreateOrGetLast<TextBlock>(obj, namedObjectMap);
                SetProperties(createResult.Item2, obj, createResult.Item1 ? lastObj : null, namedObjectMap);
                return createResult.Item2;
            }
            internal static void SetProperties(TextBlock t, JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap)
            {
                FrameworkElementHandler.SetProperties(t, obj, lastObj, namedObjectMap);
                TrySet(obj, lastObj, "text", t, (target, x, lastX) => target.Text = x.ToString());
                TrySet(obj, lastObj, "fontFamily", t, (target, x, lastX) => target.FontFamily = new FontFamily(x.ToString()));
                TrySet(obj, lastObj, "fontSize", t, (target, x, lastX) => target.FontSize = x.Value<double>());
                TrySet(obj, lastObj, "fontWeight", t, (target, x, lastX) => target.FontWeight = ParseEnum<FontWeight>(x));
            }
        }

		internal static class ButtonBaseHandler
        {
            internal static void SetProperties(ButtonBase t, JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap)
            {
                ControlHandler.SetProperties(t, obj, lastObj, namedObjectMap);
                TrySet(obj, lastObj, "content", t, (target, x, lastX) => target.Content = CreateFromState(x, lastX, namedObjectMap));
                TrySetEvent(obj, lastObj, "click", t, (target, x, lastX) => SetClickEventHandler(x.ToString(), target));
            }
            static void ClickRouter(object sender, RoutedEventArgs e)
            {
                if (Command != null)
                {
                    var map = (Dictionary<string, string>)((FrameworkElement)sender).GetValue(eventMap);
                    Command(null, new CommandEventArgs() { CommandHandlerToken = map["Click"], Sender = sender, EventArgs = e });
                }
            }
            static void SetClickEventHandler(string handlerName, ButtonBase element)
            {
                var map = (Dictionary<string, string>)element.GetValue(eventMap);
                if (map == null)
                {
                    element.SetValue(eventMap, map = new Dictionary<string, string>());
                }
                map["Click"] = handlerName;
                element.Click -= ClickRouter;
                element.Click += ClickRouter;
            }
        }

		internal static class ControlHandler
        {
            internal static void SetProperties(Control t, JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap)
            {
                FrameworkElementHandler.SetProperties(t, obj, lastObj, namedObjectMap);
                TrySet(obj, lastObj, "background", t, (target, x, lastX) => target.Background = XamlStringParse<Brush>(x));
                TrySet(obj, lastObj, "foreground", t, (target, x, lastX) => target.Foreground = XamlStringParse<Brush>(x));
                TrySet(obj, lastObj, "fontFamily", t, (target, x, lastX) => target.FontFamily = new FontFamily(x.ToString()));
                TrySet(obj, lastObj, "fontSize", t, (target, x, lastX) => target.FontSize = x.Value<double>());
                TrySet(obj, lastObj, "fontWeight", t, (target, x, lastX) => target.FontWeight = ParseEnum<FontWeight>(x));
            }
        }

        static DependencyProperty eventMap = DependencyProperty.RegisterAttached("XSEventMap", typeof(Dictionary<string, string>), typeof(FrameworkElement), PropertyMetadata.Create((object)null));
        static Dictionary<string, CreateCallback> handlers;

        public static event EventHandler<CommandEventArgs> Command;

        static Tuple<bool, T> CreateOrGetLast<T>(JObject obj, Dictionary<string, object> namedObjectMap) where T:new()
        {
            JToken name;
            if (obj.TryGetValue("name", out name))
            {
                object value;
                if (namedObjectMap.TryGetValue(name.ToString(), out value))
                {
                    if (value != null && value is T)
                    {
                        return new Tuple<bool, T>(true, (T)value);
                    }
                }
            }
            return new Tuple<bool, T>(false, new T());
        }
        static void TrySet<T>(JObject obj, JObject last, string name, T target, Setter<T> setter)
        {
            JToken tok;
            JToken tokLast = null;
            if (obj.TryGetValue(name, out tok))
            {
                if (last != null && last.TryGetValue(name, out tokLast))
                {
                    if (tokLast.ToString() == tok.ToString())
                    {
                        return; // bail early if old & new are the same
                    }
                }
                setter(target, tok, tokLast);
            }
        }
        static void TrySetEvent<T>(JObject obj, JObject last, string name, T target, Setter<T> setter)
        {
            JToken tok;
            JToken tokLast = null;
            if (obj.TryGetValue("$" + name, out tok))
            {
                if (last != null && last.TryGetValue("$" + name, out tokLast))
                {
                    if (tokLast.ToString() == tok.ToString())
                    {
                        return; // bail early if old & new are the same
                    }
                }
                setter(target, tok, tokLast);
            }
        }
        static T ParseEnum<T>(JToken v) 
        {
            return (T)Enum.Parse(typeof(T), v.ToString());
        }
        static T XamlStringParse<T>(JToken v)
        {
            return (T)Windows.UI.Xaml.Markup.XamlBindingHelper.ConvertValue(typeof(T), v.ToString());
        }
        static Dictionary<string, CreateCallback> GetHandlers()
        {
            if (handlers == null)
            {
                handlers = new Dictionary<string, CreateCallback>();
                handlers["TextBlock"] = TextBlockHandler.Create;
            }
            return handlers;
        }
        static FrameworkElement CreateFromState(JToken item, JToken lastItem, Dictionary<string, object> namedObjectMap)
        {
            if (item.Type == JTokenType.Object)
            {
                var type = item["type"].ToString();
                CreateCallback create;
                if (GetHandlers().TryGetValue(type, out create))
                {
                    return create((JObject)item, (JObject)lastItem, namedObjectMap);
                }
                return new TextBlock() { FontSize = 48, Text = "'" + type + "'Not found" };
            }
            else
            {
                return new TextBlock() { Text = item.ToString() };
            }
        }

        delegate void Setter<T>(T target, JToken value, JToken lastValue);
        delegate FrameworkElement CreateCallback(JObject obj, JObject lastObj, Dictionary<string, object> namedObjectMap);

	}
	
}

