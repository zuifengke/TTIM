/**
 * Created with JetBrains WebStorm.
 * User: XTT
 * Date: 14-2-9
 * Time: 下午7:06
 * To change this template use File | Settings | File Templates.
 */
var ws;
var bSocketCreate;
var clock;
var target="";
var isLogin = false;
var UserNameDict = {}
var ClickName = "";

 $(document).ready(function(e){

    ConnectTest()//测试连接

 	$('#commit').bind("click",function(){//发送
        var text=$("#inputText").val();
        if(text=="")
            return ;
        $('#inputText').val("");
        if(target!=""){
            text = [
                'talk',
                $('#name').val(),
                target,
                text
            ].join('&');
        }else{
            text = "all&" + text;
        }
        ws.send(text);
    });

    $("#inputText").keypress(function(e){//回车发送信息
        if(e.keyCode==13){
            $("#commit").click();
            e.preventDefault();
        }
    });

    //登录
    $("#connect").bind("click",ToggleConnectionClicked);
    $("#cancel").bind("click",ToggleConnectionClicked);
    $('.loginedMain').css("display","none");

 });


function Connect(sIP){
    if(bSocketCreate && (ws.readyState == 0 || ws.readyState == 1)){
        console.log("断开")
    }else{
        console.log("连接")
        try{
            //sIP=$("#serveid").val()
            console.log(sIP)
            if ("WebSocket" in window) {
                ws = new WebSocket("ws://" + sIP);
            }
            else if("MozWebSocket" in window) {
                ws = new MozWebSocket("ws://" + sIP);
            }    
            SocketCreated = true;
        }catch(ex){
            console.log("连接出错")
            return ;
        }
    }
    ws.onopen = WSonOpen;
    ws.onmessage = WSonMessage;
    ws.onclose = WSonClose;
    ws.onerror = WSonError;
}

function AddChatText(dInfo){//增加聊天记录
    var $target=$('.leftblock_list');
    var sUserName = dInfo["UserName"];
    var sTime = dInfo["Time"];
    var dateTime=new Date();
    if(sTime.length<=0){
        sTime = [dateTime.getHours().toString(),
                    dateTime.getMinutes().toString(),
                    dateTime.getSeconds().toString(),
                ].join(':');
    }else{
        sTime +=":" + dateTime.getSeconds().toString();
    }
    var iType = dInfo["Type"]
    if (iType == '3'){
        console.log(UserNameDict[sUserName])
        if (!UserNameDict[sUserName])
        {
            AddContact(sUserName,'#rightblock_list_ul2')
            UserNameDict[sUserName] = 1
            $('#lasttalk').click()
        }
        sUserName = '私聊 ' + sUserName
    }
    var sText = dInfo["Text"];
    $target.append(['<div class="chatshow">',
                    '<div class="chatshow_info"><p>&lt;',
                    sUserName,
                    '&gt;</p><time>',
                    sTime,
                    '</time></div><p class="text">',
                    sText,
                    '</p></div>'
                ].join(''));
    $target.scrollTop(99999);     
}

//增加联系人
function AddContact(sName,sTarget){
    var sLoginName = sName;
    if(sLoginName=="")
        sLoginName = "";
    var $rightList=$(sTarget);
    var sUserName = $('#name').val()
    if (sUserName == sName && sTarget == '#rightblock_list_ul2'){
        sLoginName = ClickName
    }


    $rightList.append(['<li class="lishow" title="',
                        sLoginName,
                        '" onclick="ClickListShow(this)"><a href="#"><span><img src="./img/photo.png" alt="" border="0"/></span><span>',
                        sLoginName,
                    '</span></a></li>'
                    ].join(''));
}

function AddTalkContact(sName){
    var sLoginName = sName;
    if(sLoginName=="")
        sLoginName = "";
}

//删除联系人
function RemoveContact(sName){
    var sUserName = $('#name').val()
    if (sUserName == sName){
        var $rightList=$('#rightblock_list_ul1');
        $rightList.empty()
        var $rightList=$('#rightblock_list_ul2');
        $rightList.empty()
        ws.close();
        return ;
    }
    sTarget = ['li[title=',sName,']'].join('')
    var $target = $(sTarget)
    $target.remove();
}

//点击列表项
function ClickListShow(obj){
    if(hasClass(obj,"lishow_click")){
        removeClass(obj,"lishow_click");
        $('#inputText').val("");
        target="";
        $('#IMTitle').text("会话");
        return;
    }
    var lLisShow = document.getElementsByTagName('li')
    for(var key in lLisShow){
        var tag = lLisShow[key];
        if(tag.className == "lishow lishow_click"){
            removeClass(tag,"lishow_click");
        }
    }
    addClass(obj, "lishow_click");
    var sText = obj.getElementsByTagName("span")[1].innerHTML
    target = sText;
    $('#IMTitle').text(target);
    ClickName = target
    AddTalkContact(target,'#rightblock_list_ul2')
}

//登录
function ToggleConnectionClicked(){
    isLogin ^= true;
    if (isLogin)
        Login()
    else
        Logout()
}

function Login(){
    sIP = $('#serveid').val()
    if(sIP == "")
        return ;
    Connect(sIP);  // 获取服务器名
    clock=setTimeout("SendUserName()",1000)
}

function Logout(){
    sText = "logout&" + $('#name').val();
    ws.send(sText)
}

function SendUserName(){
    sText = "login&" + $('#name').val();
    ws.send(sText)
    clearTimeout(clock)
}

/*******重写websocket接口*******/
function WSonOpen() {
    //lockOff();
    console.log("连接已建立")

    $('.ds-post-main').css("display","none");
    $('.loginedMain').css("display","block");
    sServer = $('#serveid').val();
    sName = $('#name').val();
    $('.loginedID').text(sServer);
    $('.loginedName').text(sName);
};

function WSonMessage(event) {
    ParseData(event.data)
};

function WSonClose() {
    console.log("关闭连接")
    Release()
};

function WSonError() {
    console.log("远程连接中断")
    Release()
};

function Release(){
    $('.ds-post-main').css("display","block");
    $('.loginedMain').css("display","none");

    var sUserName = $('#name').val()
    RemoveContact(sUserName);

    var $target=$('.leftblock_list');
    $target.empty()
}
/*******************************/

function ParseData(sTest){//解析服务端数据
    var sTextList = [];
    sTextList = sTest.split("\r\n");
    console.log("数据");
    console.log(sTextList);
    if(sTextList.length<=0) 
        return
    var cmd = sTextList.shift();
    console.log(cmd)
    switch(cmd){
        case "login":
            sName = ParseLoginOrLogout(sTextList);
            if (sName)
                AddContact(sName,'#rightblock_list_ul1');
            break;
        case "chat":
            dInfo = ParseChat(sTextList);
            console.log(dInfo)
            if(dInfo)
                AddChatText(dInfo);
            break;
        case "logout":
            sName = ParseLoginOrLogout(sTextList);
            if (sName)
                RemoveContact(sName);
            break;
        default:
            console.log("无该指令");
            break;
    }
}

function ParseLoginOrLogout(sTextList){
    if(sTextList.length<=0) return null;
    var sName = sTextList.shift()
    return sName;
}

function ParseChat(sTextList){//解析聊天指令内容
    if(sTextList.length<=0) return null;
    var dInfo = {};
    var sNameandTime = sTextList.shift()
    var sType = sTextList.shift()
    dInfo["Type"] = sType
    tmpList = sNameandTime.split(' ');
    dInfo["UserName"]=tmpList[0]?tmpList[0]:"No Name";
    dInfo["Time"]=tmpList[1]?tmpList[1]:"";
    var sText = "";
    for(var i=0;i<sTextList.length;i++){
        sText+=sTextList[i].toString()+"\n";
    }
    dInfo["Text"]=sText;
    return dInfo;
}

function ConnectTest(){//websocket测试
    var bWebSocketsExist = true;
    try {
        var dummy = new WebSocket("ws://localhost:51888/test");
    } catch (ex) {
        try{
            webSocket = new MozWebSocket("ws://localhost:51888/test");
        } catch(ex) {
             bWebSocketsExist = false;
        }
    }
    if(bWebSocketsExist){
        console.log("您的浏览器支持WebSocket. 您可以尝试连接到聊天服务器!")
        return true;
    }else{
        console.log("您的浏览器不支持WebSocket。请选择其他的浏览器再尝试连接服务器。")
        return false;
    }  
}