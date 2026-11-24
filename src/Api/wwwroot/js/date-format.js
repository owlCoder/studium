export function formatLocalDateTime(value) {
    const date = value instanceof Date ? value : new Date(value);
    if (value === null || value === undefined || value === "" || Number.isNaN(date.getTime())) {
        throw new TypeError("Datum nije ispravan.");
    }

    const day = pad(date.getDate());
    const month = pad(date.getMonth() + 1);
    const year = date.getFullYear();
    const hours = pad(date.getHours());
    const minutes = pad(date.getMinutes());

    return `${day}.${month}.${year}. ${hours}:${minutes}`;
}

function pad(number) {
    return String(number).padStart(2, "0");
}
