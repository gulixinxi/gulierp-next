#!/bin/bash
# GuliERP 启动脚本：postgres + watchdog + API + 前端
# 用法：./start-gulicrm.sh

PGDATA=/home/user/Doubao/chats/38447590217562626/pgdata
PGCTL=/home/user/.local/lib/python3.12/site-packages/pgserver/pginstall/bin/pg_ctl
API_DIR=/home/user/Doubao/chats/38447590217562626/gulierp/apps/api/GuliERP.Api
WEB_DIR=/home/user/Doubao/chats/38447590217562626/gulierp/apps/web
WATCHDOG=/home/user/Doubao/chats/38447590217562626/postgres-watchdog.sh
export PATH="$PATH:$HOME/.dotnet"

# 1. 启动 PostgreSQL（用 pg_ctl start -w，守护进程方式，PPID=1）
if ! $PGCTL -D "$PGDATA" status >/dev/null 2>&1; then
    $PGCTL -D "$PGDATA" -l "$PGDATA/logfile.log" -o "-k $PGDATA -p 5432" start -w
    echo "PostgreSQL 已启动"
else
    echo "PostgreSQL 已在运行"
fi

# 2. 启动 PostgreSQL watchdog（挂了自动重启）
if ! pgrep -f "postgres-watchdog" > /dev/null 2>&1; then
    setsid nohup "$WATCHDOG" > /dev/null 2>&1 < /dev/null &
    disown 2>/dev/null
    echo "Watchdog 已启动"
else
    echo "Watchdog 已在运行"
fi

# 3. 启动 API（直接运行编译好的 DLL，比 dotnet run 快）
if ! curl -s -o /dev/null http://localhost:5000/api/v1/auth/csrf 2>/dev/null; then
    cd "$API_DIR"
    setsid env ASPNETCORE_ENVIRONMENT=Development "$HOME/.dotnet/dotnet" bin/Debug/net10.0/GuliERP.Api.dll --urls http://0.0.0.0:5000 > /tmp/gulicrm-api.log 2>&1 < /dev/null &
    disown 2>/dev/null
    echo "API 启动中 (PID=$!)"
else
    echo "API 已在运行"
fi

# 4. 启动前端
if ! curl -s -o /dev/null http://localhost:5173/ 2>/dev/null; then
    cd "$WEB_DIR"
    setsid npx vite --host 0.0.0.0 --port 5173 > /tmp/gulicrm-web.log 2>&1 < /dev/null &
    disown 2>/dev/null
    echo "前端启动中 (PID=$!)"
else
    echo "前端已在运行"
fi

# 5. 等待 API 就绪
echo "等待 API 就绪..."
for i in $(seq 1 30); do
    sleep 2
    if curl -s -o /dev/null http://localhost:5000/api/v1/auth/csrf 2>/dev/null; then
        echo "API 就绪 (约 $((i*2)) 秒)"
        break
    fi
done

curl -s -o /dev/null -w "API:%{http_code} " http://localhost:5000/api/v1/auth/csrf
curl -s -o /dev/null -w "前端:%{http_code}\n" http://localhost:5173/
echo ""
echo "=== 启动完成 ==="
echo "前端地址: http://localhost:5173"
echo "API 地址: http://localhost:5000"
echo "登录账号: admin / ChangeMe!2026"
echo "Watchdog 日志: $PGDATA/watchdog.log"
