#!/bin/bash
# PostgreSQL watchdog：定期检查 postgres 状态，挂了自动重启
# 用法：nohup ./postgres-watchdog.sh &

PGDATA="/home/user/Doubao/chats/38447590217562626/pgdata"
PGCTL="/home/user/.local/lib/python3.12/site-packages/pgserver/pginstall/bin/pg_ctl"
LOGFILE="$PGDATA/watchdog.log"
CHECK_INTERVAL=30  # 秒

echo "[$(date '+%Y-%m-%d %H:%M:%S')] Watchdog 启动，检查间隔 ${CHECK_INTERVAL}s" >> "$LOGFILE"

while true; do
    if ! $PGCTL -D "$PGDATA" status > /dev/null 2>&1; then
        echo "[$(date '+%Y-%m-%d %H:%M:%S')] PostgreSQL 未运行，尝试重启..." >> "$LOGFILE"
        $PGCTL -D "$PGDATA" -l "$PGDATA/logfile.log" -o "-k $PGDATA -p 5432" start -w >> "$LOGFILE" 2>&1
        if $PGCTL -D "$PGDATA" status > /dev/null 2>&1; then
            echo "[$(date '+%Y-%m-%d %H:%M:%S')] PostgreSQL 重启成功" >> "$LOGFILE"
        else
            echo "[$(date '+%Y-%m-%d %H:%M:%S')] PostgreSQL 重启失败" >> "$LOGFILE"
        fi
    fi
    sleep $CHECK_INTERVAL
done
