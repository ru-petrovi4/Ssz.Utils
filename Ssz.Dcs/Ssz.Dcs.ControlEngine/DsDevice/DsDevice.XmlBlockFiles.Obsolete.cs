using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ssz.Dcs.ControlEngine
{
    public partial class DsDevice : IDisposable
    {   
        #region private functions

        private List<DsModule> LoadModulesFromXmlObsolete(ILogger? userFriendlyLogger, string xmlDsBlockFileFullName)
        {
            var result = new List<DsModule>();
            
            var doc = new XmlDocument();
            doc.Load(xmlDsBlockFileFullName);

            XmlNode? dataroot = doc.ChildNodes[1];
            if (dataroot is null) return result;
            XmlNodeList? tagList = ReadHeader(dataroot);
            if (tagList is null) return result;
            //bool isCreatedByLB = false;
            foreach (XmlNode tag in tagList)
            {
                if (tag.Name == "Prog")
                {
                    //string? s = tag.Attributes?.GetNamedItem("ID")?.Value;
                    //if (s == "LogicBuilder")
                    //    isCreatedByLB = true;
                }
                if (tag.Name != "Tag")
                    continue;
                string? vType = tag.Attributes?.GetNamedItem("vType")?.Value;
                if (vType != "4097")
                    continue;

                DsModule? dsModule = ImportPage(tag);
                if (dsModule is not null) result.Add(dsModule);
            }

            return result;
        }

        private XmlNodeList? ReadHeader(XmlNode dataroot)
        {
            //bool isCreatedByLB = false;
            foreach (XmlNode tag in dataroot.ChildNodes)
            {
                if (tag.Name == "Prog")
                {
                    //string? s = tag.Attributes?.GetNamedItem("ID")?.Value;
                    //if (s == "LogicBuilder")
                    //    isCreatedByLB = true;
                }
                if (tag.Name == "Vars")
                    return tag.ChildNodes;

                if (tag.Name == "Tag")
                    return dataroot.ChildNodes;
            }
            return null;
        }

        private DsModule? ImportPage(XmlNode tag)
        {
            return null;
            //bool handCoord = true;

            //int blY = 0;
            //if (tag.Attributes?.GetNamedItem("X") is not null)
            //{
            //    blY = (int)Convert.ToDouble(tag.Attributes?.GetNamedItem("Y")?.Value);
            //    handCoord = false;
            //}
            //string? pageName = tag.Attributes?.GetNamedItem("vName")?.Value;
            //if (pageName is null) return null;
            //if (pageName.StartsWith("bl"))
            //    pageName = pageName.Substring(2);

            //var module = new DsModule(pageName, _processDataAccessProvider);

            //XmlNode? elements = tag.ChildNodes[0];
            //if (elements is null) return null;

            //#region Импорт шейпов

            //foreach (XmlNode element in elements.ChildNodes)
            //{
            //    if (element.Name != "Element") continue;
            //    DsBlockBase? importedDsBlock = ImportShape(element);
            //    if (importedDsBlock is null)
            //        continue;                
            //}
            //if (graphControl.ShapesCount == 0)
            //    return;

            //#endregion
            

            //#region Импорт коннекторов (Новый)

            //foreach (XmlNode element in elements.ChildNodes)
            //{
            //    XmlNode conns = null;
            //    foreach (XmlNode n in element.ChildNodes)
            //    {
            //        if (n.Name == "Conns")
            //        {
            //            conns = n;
            //            break;
            //        }
            //    }
            //    if ((conns is null) || (conns.ChildNodes.Count == 0) || element.Attributes is null)
            //        continue;
            //    _LogForm.AddShapeText(
            //        "Импорт коннекторов для элемента " + element.Attributes.GetNamedItem("Name").Value, null);
            //    foreach (XmlNode conn in conns.ChildNodes)
            //    {
            //        if (conn.Attributes is null)
            //            continue;
            //        var beginSignal = conn.Attributes.GetNamedItem("Begin").Value;
            //        var endSignal = conn.Attributes.GetNamedItem("End").Value;
            //        _LogForm.AddShapeText(
            //            "Импорт коннекторa " + beginSignal + " - " + element.Attributes.GetNamedItem("Name").Value + "." +
            //            endSignal, null);
            //        if (endSignal.IsNullOrEmpty())
            //        {
            //            _LogForm.AddShapeError("Имя конечного сигнала отсутствует ", null);
            //            continue;
            //        }
            //        var shapeEnd = graphControl.GetElement(element.Attributes.GetNamedItem("Name").Value);
            //        if (shapeEnd is null)
            //        {
            //            _LogForm.AddShapeError("Не найден элемент " + element.Attributes.GetNamedItem("Name").Value,
            //                                   null);
            //            continue;
            //        }

            //        if (beginSignal.StartsWith("."))
            //        {
            //            var currentCon = new Connection(Point.Empty, Point.Empty);

            //            var bDotPos = beginSignal.IndexOf(".", StringComparison.Ordinal);
            //            var eDotPos = beginSignal.IndexOf(".", bDotPos + 1, StringComparison.Ordinal);
            //            if ((eDotPos - bDotPos - 1) < 1)
            //            {
            //                _LogForm.AddShapeError("Имя начального сигнала отсутствует ", null);
            //                continue;
            //            }
            //            var beginElement = beginSignal.Substring(bDotPos + 1, eDotPos - bDotPos - 1);
            //            var shapeBegin = graphControl.GetElement(beginElement);

            //            if (shapeBegin is null)
            //            {
            //                _LogForm.AddShapeError("Не найден элемент " + beginElement, null);
            //                continue;
            //            }

            //            var beginCr = beginSignal.Substring(eDotPos + 1);
            //            var b = shapeBegin.GetConnector(beginCr);
            //            if (b is null)
            //            {
            //                _LogForm.AddShapeError("Не найден параметр элемента " + shapeBegin.Name + " Param = " + beginCr, null);
            //                continue;
            //            }
            //            var e = shapeEnd.GetConnector(endSignal);
            //            if (e is null)
            //            {
            //                _LogForm.AddShapeError("Не найден параметр элемента " + shapeEnd.Name + " Param = " + endSignal, null);
            //                continue;
            //            }
            //            b.AttachConnector(currentCon.From);
            //            e.AttachConnector(currentCon.To);
            //            currentCon.Site = graphControl;
            //            graphControl.Add(currentCon, false);
            //        }
            //        else
            //        {
            //            shapeEnd.OtherConns[endSignal] = beginSignal;
            //        }
            //    }
            //}

            //#endregion

            //#region Импорт коннекторов (Cтарый)

            //var oldConns = tag.ChildNodes[1];
            //if (oldConns is not null)
            //{
            //    _LogForm.AddShapeText("Импорт коннекторов для группы " + graphControl.Parent.Text, null);

            //    foreach (XmlNode cn in oldConns)
            //    {
            //        string b = cn.Attributes.GetNamedItem("Begin").Value;
            //        int st = b.IndexOf(".");
            //        int st2 = b.IndexOf(".", st + 1);
            //        string bname = b.Substring(st + 1, st2 - st - 1);
            //        string bcon = b.Substring(st2 + 1);
            //        string bblock = b.Substring(0, b.IndexOf("."));

            //        string e = cn.Attributes.GetNamedItem("End").Value;
            //        st = e.IndexOf(".");
            //        st2 = e.IndexOf(".", st + 1);
            //        string ename = e.Substring(st + 1, st2 - st - 1);
            //        string econ = e.Substring(st2 + 1);

            //        if (bblock != graphControl.Parent.Text) ///Начало не из этого блока
            //        {
            //            if (graphControl.GetElement("In_" + b.Replace(".", "_")) is null) ///Ёще не добавлен
            //            {
            //                var l = new LinkIn(graphControl);
            //                try
            //                {
            //                    l.ID = b.Replace(".", "_");
            //                }
            //                catch
            //                {
            //                }
            //                l.Name = "In_" + b.Replace(".", "_");
            //                l.Location = new Point(0, 0);
            //                yLeft = yLeft + l.Height + dltY;
            //                graphControl.Add(l, false);
            //                _Links.Add(b);
            //            }
            //            bname = "In_" + b.Replace(".", "_");
            //            bcon = "PV";
            //        }
            //        ShapeBase sb = graphControl.GetElement(bname);
            //        ShapeBase se = graphControl.GetElement(ename);
            //        if (sb is null)
            //        {
            //            _LogForm.AddShapeError("Не найден элемент " + bname, null);
            //            continue;
            //        }
            //        if (se is null)
            //        {
            //            _LogForm.AddShapeError("Не найден элемент " + ename, null);
            //            continue;
            //        }
            //        var c = new Connection(Point.Empty, Point.Empty);


            //        try
            //        {
            //            sb.GetConnector(bcon).AttachConnector(c.From);
            //        }
            //        catch (Exception e1)
            //        {
            //            _LogForm.AddShapeError("Не найден вход " + e1.Message);
            //            _LogForm.AddShapeError("Не найден вход " + bname + "." + bcon);
            //        }
            //        try
            //        {
            //            se.GetConnector(econ).AttachConnector(c.To);
            //        }
            //        catch (Exception e1)
            //        {
            //            _LogForm.AddShapeError("Не найден вход " + e1.Message);
            //            _LogForm.AddShapeError("Не найден вход " + ename + "." + econ);
            //        }

            //        graphControl.Add(c, false);
            //        c.Site = graphControl;
            //    }
            //}

            //#endregion
        }

        //private DsBlockBase ImportShape(XmlNode el)
        //{
        //    ShapeBase shape = CreateShape(el, g);
        //    if (shape is null)
        //        return null;

        //    g.Add(shape, false);

        //    XmlNode parameters = el.ChildNodes[0];

        //    PropertyInfo countProp = shape.GetType().GetProperty("COUNT");
        //    if (countProp is not null)
        //    {
        //        _LogForm.AddShapeText("Найден массив", null);
        //        XmlNode countNode =
        //            parameters.ChildNodes.Cast<XmlNode>().FirstOrDefault(
        //                node => node.Attributes["Name"].Value.Replace(">", "") == "COUNT");
        //        if (countNode is not null)
        //        {
        //            object length = Convert.ChangeType(countNode.Attributes["Val"].Value, countProp.PropertyType);
        //            countProp.SetValue(shape, length, null);
        //            _LogForm.AddShapeText("Задана длинна " + length, null);
        //        }
        //    }

        //    var props = shape.GetType().GetProperties().Where(p => p.IsDefined(typeof(ExportToXmlAttribute), true));
        //    var xmlProps = new Dictionary<string, PropertyInfo>();
        //    foreach (var prop in props)
        //    {
        //        var attr = (ExportToXmlAttribute)Attribute.GetCustomAttribute(prop, typeof(ExportToXmlAttribute));
        //        if (attr.F)
        //        {
        //            xmlProps[attr.Name.ToUpperInvariant()] = prop;
        //        }
        //    }

        //    foreach (XmlNode param in parameters.ChildNodes)
        //    {
        //        var paramOriginalName = param.Attributes["Name"].Value;
        //        Match match = ArrayRegex.Match(paramOriginalName.Replace(">", ""));
        //        string paramName = match.Groups["name"].Value.ToUpperInvariant();
        //        string vl = null;
        //        var val = param.Attributes.GetNamedItem("Val");
        //        if (val is not null) vl = val.Value;
        //        xmlProps.TryGetValue(paramName, out PropertyInfo prop);
        //        if (prop is null) // Unknown property
        //        {
        //            shape.UnknownParams[paramOriginalName] = vl;
        //            continue;
        //        }
        //        if (!prop.CanWrite)
        //        {
        //            _LogForm.AddShapeText("Поле " + paramName + " доступно только для чтения.", null);
        //            continue;
        //        }
        //        try
        //        {
        //            _LogForm.AddShapeText("инициализация свойства " + paramName, null);

        //            if (string.IsNullOrEmpty(vl))
        //                continue;
        //            switch (vl.ToUpperInvariant())
        //            {
        //                case "FALSE":
        //                    vl = "0";
        //                    break;
        //                case "TRUE":
        //                    vl = "1";
        //                    break;
        //            }

        //            if (prop.PropertyType.IsEnum)
        //            {
        //                object value = Enum.Parse(prop.PropertyType, vl);
        //                prop.SetValue(shape, value, null);
        //            }
        //            else if (prop.PropertyType == typeof(bool))
        //                prop.SetValue(shape, ConvertToBoolean(vl), null);
        //            else if (prop.PropertyType.IsArray)
        //            {
        //                int idx = int.Parse(match.Groups["idx"].Value);
        //                _LogForm.AddShapeText("Индекс " + idx, null);
        //                var array = (Array)prop.GetValue(shape, null);
        //                Type arrayElementType = array.GetType().GetElementType();
        //                int idxToSet = idx - 1;
        //                if (idxToSet > array.GetLength(0) - 1)
        //                {
        //                    Array newA = Array.CreateInstance(arrayElementType, idxToSet + 1);
        //                    array.CopyTo(newA, 0);
        //                    array = newA;
        //                }
        //                if (arrayElementType == typeof(bool))
        //                    array.SetValue(ConvertToBoolean(vl), idx - 1);
        //                else
        //                    array.SetValue(Convert.ChangeType(vl, arrayElementType), idxToSet);
        //                prop.SetValue(shape, array, null);
        //            }
        //            else
        //            {
        //                object value = Convert.ChangeType(vl, prop.PropertyType);
        //                prop.SetValue(shape, value, null);
        //            }
        //        }
        //        catch (WarningException w)
        //        {
        //            _LogForm.AddShapeWarning(w.Message, null);
        //        }
        //        catch (Exception e1)
        //        {
        //            _LogForm.AddShapeError(e1.Message, null);
        //            _LogForm.AddShapeError("Ошибка инициализации свойства", null);
        //        }
        //    }

        //    XmlNode eventMessagesXmlNode = null;
        //    foreach (XmlNode n in el.ChildNodes)
        //    {
        //        if (n.Name == "EventMessages")
        //        {
        //            eventMessagesXmlNode = n;
        //            break;
        //        }
        //    }
        //    if (eventMessagesXmlNode is not null)
        //    {
        //        foreach (XmlNode em in eventMessagesXmlNode.ChildNodes)
        //        {
        //            if (em.Name == "EventMessage")
        //            {
        //                try
        //                {
        //                    var eventMessage = new EventMessage();

        //                    eventMessage.ParamName = em.Attributes.GetNamedItem("ParamName").Value;
        //                    eventMessage.ParamValue = em.Attributes.GetNamedItem("ParamValue").Value;
        //                    eventMessage.ObjectPropName = em.Attributes.GetNamedItem("ObjectPropName").Value;
        //                    eventMessage.ObjectName = em.Attributes.GetNamedItem("ObjectName").Value;
        //                    eventMessage.ObjectValueName = em.Attributes.GetNamedItem("ObjectValueName").Value;

        //                    shape.EventMessages.Add(eventMessage);
        //                }
        //                catch (Exception ex)
        //                {
        //                    MessageBox.Show(ex.StackTrace);
        //                }
        //            }
        //        }
        //    }

        //    return shape;
        //}

        //private DsBlockBase? CreateShape(XmlNode el)
        //{
        //    string tp = el.Attributes!.GetNamedItem("Type")!.Value ?? "";
        //    XmlNode? xmlShID = el.Attributes!.GetNamedItem("ShapeID");
        //    string shapeID = string.Empty;
        //    if (xmlShID is not null)
        //        shapeID = xmlShID.Value ?? "";
        //    ShapeBase shape = null;
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(shapeID))
        //        {
        //            var T = (ShapeTypes)Enum.Parse(typeof(ShapeTypes), shapeID);
        //            if (T == ShapeTypes.ToOPC && el.InnerXml.Contains("OPCTAG"))
        //            {
        //                el.InnerXml = el.InnerXml.Replace("End=\"PV\"", "End=\"REMOTE_PV\"");
        //                shape = ShapeVars.ShapeBuilder.CreateShape(ShapeTypes.FromOPC, g);
        //            }
        //            else
        //            {
        //                shape = ShapeVars.ShapeBuilder.CreateShape(T, g);
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }

        //    if (shape is null)
        //        switch (tp.ToUpper())
        //        {
        //            case "BLOCK.GET":
        //            case "BLOCK.I":
        //                shape = new FromDsBlock(g);
        //                break;
        //            case "BLOCK.SET":
        //            case "BLOCK.O":
        //                shape = new ToDsBlock(g);
        //                break;

        //            case "LOGIC.AND":
        //            case "LOGIC.OR":
        //                foreach (XmlNode param in el.ChildNodes[0].ChildNodes)
        //                {
        //                    XmlNode tNd = param.Attributes.GetNamedItem("Name");
        //                    if (tNd is not null && tNd.Value == "N")
        //                    {
        //                        shape = new CountOR(g);
        //                        break;
        //                    }
        //                }
        //                if (shape is null)
        //                    shape = new LogicDsBlock(g);

        //                break;
        //            case "UTILITY.NUMERIC":
        //                XmlNode prms = el.ChildNodes[0];
        //                foreach (XmlNode prm in prms.ChildNodes)
        //                {
        //                    if (prm.Attributes.GetNamedItem("Name").Value.Replace(">", "") == "PV")
        //                    {
        //                        string vl = prm.Attributes.GetNamedItem("Val").Value.ToLower();
        //                        if (vl == "true" || vl == "false")
        //                        {
        //                            shape = new BoolValue(g);
        //                            _LogForm.AddShapeText("Выбран тип Bool Value", null);
        //                        }
        //                        else
        //                        {
        //                            shape = new DoubleValue(g);
        //                            _LogForm.AddShapeText("Выбран тип Double Value", null);
        //                        }
        //                        break;
        //                    }
        //                }
        //                break;
        //            case "UTILITY.NUMERICREAL":
        //                shape = new DoubleValue(g);
        //                break;
        //            case "UTILITY.FLAG":
        //                shape = new DoubleValue(g);
        //                break;
        //            case "LOGIC.SELREAL":
        //                shape = new SelReal(g);
        //                break;
        //            case "LOGIC.SELREALEX":
        //                shape = new CountSelReal(g);
        //                break;
        //            case "LOGIC.SEL":
        //                shape = new SelBoolean(g);
        //                break;
        //            case "LOGIC.RS":
        //                shape = new RSDsBlock(g);
        //                break;
        //            case "LOGIC.ONPULSE":
        //            case "LOGIC.OFFPULSE":
        //                shape = new Pulse(g);
        //                break;
        //            case "LOGIC.ONDELAY":
        //            case "LOGIC.OFFDELAY":
        //                shape = new shDelay(g);
        //                break;
        //            case "LOGIC.DELAY":
        //                shape = new Delay(g);
        //                break;
        //            case "LOGIC.DELAYREAL":
        //                shape = new DelayReal(g);
        //                break;
        //            case "LOGIC.EQ":
        //            case "LOGIC.LE":
        //            case "LOGIC.GE":
        //                shape = new Comparator(g);
        //                break;
        //            case "LOGIC.NOT":
        //                shape = new NotDsBlock(g);
        //                _LogForm.AddShapeText("Выбран тип Not DsBlock", null);
        //                break;
        //            case "LOGIC.MUL":
        //                shape = new shMultiplication(g);
        //                break;
        //            case "LOGIC.MIN":
        //                shape = new shMin(g);
        //                break;
        //            case "LOGIC.MAX":
        //                shape = new shMax(g);
        //                break;
        //            case "LOGIC.DIV":
        //                shape = new shDivision(g);
        //                break;
        //            case "LOGIC.SUM":
        //                shape = new shSummator(g);
        //                break;
        //            case "BLOCK.OUTLET":
        //            case "BLOCK.OUTLETREAL":
        //                shape = new LinkOut(g);
        //                break;
        //            case "BLOCK.INLET":
        //            case "BLOCK.INLETREAL":
        //                shape = new LinkIn(g);
        //                break;
        //            case "UTILITY.TIMER":
        //                shape = new shTimer(g);
        //                break;
        //            case "UTILITY.LC":
        //                shape = new LCElementViewModel(g);
        //                break;
        //            case "UTILITY.SHIBER":
        //                shape = new shValveManager(g);
        //                break;
        //            case "UTILITY.COUNTER":
        //                shape = new shCounter(g);
        //                break;
        //            case "BLOCK.PORT":
        //                shape = el.ChildNodes[1].ChildNodes.Count > 0 ? new shToOPC(g) : new shToOPC(g);
        //                break;

        //            default:
        //                _LogForm.AddShapeLevelError("Неизвестный тип блока " + tp, null);
        //                break;
        //        }
        //    if (shape is not null)
        //    {
        //        shape.Name = el.Attributes.GetNamedItem("Name").Value;

        //        if (el.Attributes.GetNamedItem("Desc") is not null)
        //            shape.Desc = el.Attributes.GetNamedItem("Desc").Value;
        //        if (el.Attributes.GetNamedItem("Data") is not null)
        //            shape.Data = el.Attributes.GetNamedItem("Data").Value;

        //        try
        //        {
        //            shape.Type = tp;
        //        }
        //        catch (WarningException w)
        //        {
        //            _LogForm.AddShapeWarning(w.Message, shape);
        //        }
        //        catch
        //        {
        //            _LogForm.AddShapeError("Невозможно присвоить тип элементу " + tp, shape);
        //        }
        //    }
        //    return shape;
        //}

        #endregion
    }
}
