import { extractErrorMessage } from '@/lib/utils';

interface ErrorMessageProps {
  error:   unknown;
  title?:  string;
  retry?:  () => void;
}

export function ErrorMessage({ error, title = 'Failed to load data', retry }: ErrorMessageProps) {
  const message = extractErrorMessage(error);

  return (
    <div className="rounded-md border border-red-200 bg-red-50 p-4">
      <div className="flex items-start gap-3">
        <span className="text-red-500 text-lg leading-none mt-0.5">⚠</span>
        <div className="flex-1">
          <p className="text-sm font-medium text-red-800">{title}</p>
          <p className="mt-1 text-sm text-red-600">{message}</p>
        </div>
        {retry && (
          <button
            onClick={retry}
            className="text-sm font-medium text-red-700 hover:text-red-900 underline"
          >
            Retry
          </button>
        )}
      </div>
    </div>
  );
}

export function EmptyState({ message = 'No data found.' }: { message?: string }) {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-gray-400">
      <span className="text-4xl mb-3">📭</span>
      <p className="text-sm">{message}</p>
    </div>
  );
}

