window.blazorExtensions = {
    SendLocalEmail: function (mailto, subject, body, firstName, lastName, contactNumber) {

        var link = document.createElement('a');
        var uri = "mailto:" + mailto + "?";
        if (!isEmpty(subject)) {
            uri = uri + "subject=" + subject;
        }

        if (!isEmpty(body)) {
            if (!isEmpty(subject)) { // We already appended one querystring parameter, add the '&' separator
                uri = uri + "&"
            }
            uri = uri + "body=" + "Dear Katie\n" + body + "\n\n" + "--\nMany thanks\n" + firstName + " " + lastName + " \n" + contactNumber;
        }

        uri = encodeURI(uri);
        uri = uri.substring(0, 2000); // Avoid exceeding querystring limits.
        console.log('Clicking SendLocalEmail link:', uri);

        link.href = uri;
        document.body.appendChild(link); // Needed for Firefox
        link.click();
        document.body.removeChild(link);
    }
};

// Scroll to an element by ID with smooth animation
window.scrollToElement = function (elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    }
};

function isEmpty(str) {
    return (!str || str.length === 0);
}

// Check if a carousel exists in the DOM
window.carouselExists = function (id) {
    const exists = document.getElementById(id) !== null;
    console.log(`Carousel ${id} exists: ${exists}`);
    return exists;
};