export function mapApiError(error: unknown): string {
    if (error instanceof Error) {
        if (error.message.includes("400")) {
            return "Please review your input and try again.";
        }

        if (error.message.includes("404")) {
            return "Requested record was not found.";
        }
    }

    return "Something went wrong. Please try again.";
}
