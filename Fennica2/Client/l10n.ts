/**
 * Localization for frontend.  Most of localization is server-size and is handled in Resources/Fennica.*.resx
 */
const strings: {[lang: string]: {[key: string]: string}} = {
    "ru": {
        "Published on": "Опубликовано",
        "Read more": "Подробнее",
        "Zoom in to see less notable places": "Приблизьте карту, чтобы увидеть больше мест",
        "See also": "См. также",
        "Finland": "Финляндия",
        "Other": "Прочее"
    },
    "fi": {
        "Published on": "Julkaistu",
        "Read more": "Lue lisää",
        "Zoom in to see less notable places": "Zoomaa nähdäksesi lisää paikkoja",
        "See also": "Ks. myös",
    },
};

export default function _(str: string, targetLang: string) {
    if (strings[targetLang] && strings[targetLang][str]) {
        return strings[targetLang][str];
    }
    return str;
}
