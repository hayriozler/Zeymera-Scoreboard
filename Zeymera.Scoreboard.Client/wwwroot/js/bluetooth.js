// Web Bluetooth only works with a custom GATT service - browsers block the
// standard HID-over-GATT service, so a generic BLE keyboard/pedal cannot be
// used here. This targets the Nordic UART Service convention commonly used
// by ESP32/Arduino BLE projects. Replace the UUIDs below to match your
// remote's firmware. Each notification's UTF-8 text is forwarded as-is to
// C# (OnBluetoothCommand), which accepts either a bare command name (e.g.
// "IncrementPoints") or the same JSON envelope the WebSocket control channel
// uses, e.g. {"command":"UpsertPlayer","payload":{"id":1,"nickname":"..."}} -
// see ScoreboardCommandParser.cs for the exact format. Note a full player
// photo (base64) is a lot of data for typical BLE notify MTU sizes - that
// payload is realistically better suited to the WebSocket channel unless
// your firmware chunks/reassembles large messages itself.
const SERVICE_UUID = "6e400001-b5a3-f393-e9a9-e50e24dcca9e";
const NOTIFY_CHARACTERISTIC_UUID = "6e400003-b5a3-f393-e9a9-e50e24dcca9e";

let device = null;
let characteristic = null;

export function isSupported() {
    return "bluetooth" in navigator;
}

export async function connect(dotNetRef) {
    if (!isSupported()) {
        throw new Error("Web Bluetooth is not supported in this browser.");
    }

    device = await navigator.bluetooth.requestDevice({
        filters: [{ services: [SERVICE_UUID] }]
    });

    device.addEventListener("gattserverdisconnected", () => {
        characteristic = null;
        dotNetRef.invokeMethodAsync("OnBluetoothDisconnected");
    });

    const server = await device.gatt.connect();
    const service = await server.getPrimaryService(SERVICE_UUID);
    characteristic = await service.getCharacteristic(NOTIFY_CHARACTERISTIC_UUID);

    await characteristic.startNotifications();
    characteristic.addEventListener("characteristicvaluechanged", (event) => {
        const value = new TextDecoder("utf-8").decode(event.target.value).trim();
        if (value) {
            dotNetRef.invokeMethodAsync("OnBluetoothCommand", value);
        }
    });

    dotNetRef.invokeMethodAsync("OnBluetoothConnected", device.name ?? "BLE remote");
}

export function disconnect() {
    if (device?.gatt?.connected) {
        device.gatt.disconnect();
    }
    device = null;
    characteristic = null;
}
