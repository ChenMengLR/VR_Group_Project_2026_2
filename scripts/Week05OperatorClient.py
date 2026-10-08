"""Local MCP SSE client for the bundled Meta XR Operator during Play Mode."""
import base64
import json
import queue
import threading
import urllib.request
from pathlib import Path

events = queue.Queue()
endpoint = queue.Queue()


def read_sse():
    with urllib.request.urlopen("http://127.0.0.1:8720/sse", timeout=3600) as response:
        event = ""
        data = []
        for raw in response:
            line = raw.decode("utf-8").rstrip("\r\n")
            if not line:
                if event == "endpoint":
                    endpoint.put("http://127.0.0.1:8720" + "".join(data))
                elif event == "message":
                    try:
                        events.put(json.loads("".join(data)))
                    except json.JSONDecodeError:
                        pass
                event, data = "", []
            elif line.startswith("event: "):
                event = line[7:]
            elif line.startswith("data: "):
                data.append(line[6:])


threading.Thread(target=read_sse, daemon=True).start()
post_url = endpoint.get(timeout=30)
next_id = 0


def send(method, params=None, wants_response=True):
    global next_id
    payload = {"jsonrpc": "2.0", "method": method}
    if params is not None:
        payload["params"] = params
    if wants_response:
        next_id += 1
        payload["id"] = next_id
    request = urllib.request.Request(
        post_url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    with urllib.request.urlopen(request, timeout=30):
        pass
    if wants_response:
        while True:
            item = events.get(timeout=60)
            if item.get("id") == next_id:
                return item


print(json.dumps(send("initialize", {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {"name": "week05-verification", "version": "1.0"},
}), ensure_ascii=False), flush=True)
send("notifications/initialized", wants_response=False)
tools = send("tools/list").get("result", {}).get("tools", [])
print("TOOLS " + json.dumps([tool["name"] for tool in tools], ensure_ascii=False), flush=True)

while True:
    try:
        command = input()
    except EOFError:
        break
    if command == "quit":
        break
    if command.startswith("schema "):
        name = command[7:].strip()
        print(json.dumps(next((tool for tool in tools if tool["name"] == name), {}), ensure_ascii=False), flush=True)
        continue
    try:
        params = json.loads(command)
        result = send("tools/call", {"name": params["name"], "arguments": params.get("arguments", {})})
        if "save_image" in params:
            image = next((item for item in result.get("result", {}).get("content", [])
                          if item.get("type") == "image"), None)
            if image is None:
                print("NO_IMAGE " + json.dumps(result, ensure_ascii=False)[:500], flush=True)
                continue
            path = Path(params["save_image"])
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(base64.b64decode(image["data"]))
            print(f"IMAGE_SAVED {path} {path.stat().st_size} bytes", flush=True)
            continue
        print(json.dumps(result, ensure_ascii=False), flush=True)
    except Exception as exc:
        print("CLIENT_ERROR " + repr(exc), flush=True)
