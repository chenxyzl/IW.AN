using System.Threading.Tasks;

namespace Base.Player;

//启动 加载db数据 ~只能处理序列化相关不要有业务逻辑
public delegate Task LoadDelegate<T>(T a) where T : BaseActor;

//开始 第一个tick开始前  @param:crossDay 是否跨天
public delegate Task StartDelegate<T>(T a, bool crossDay) where T : BaseActor;

//停止前
public delegate Task PreStopDelegate<T>(T a) where T : BaseActor;

//停止
public delegate Task StopDelegate<T>(T a) where T : BaseActor;
//
// //每一帧 @param:now 参数为服务器当前时间
// public delegate Task OnlineDelegate<T>(T a, bool newLogin, long lastLogoutTime) where T : BaseActor;
//
// //下线
// public delegate Task OfflineDelegate<T>(T a) where T : BaseActor;

//tick
public delegate Task TickDelegate<T>(T a, long dt) where T : BaseActor;