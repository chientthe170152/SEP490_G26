export function Home() {
  return (
    <div className="p-8">
      <h1 className="text-3xl font-semibold">MTCA web — Hello World</h1>
      <p className="mt-2 text-gray-600">App: {import.meta.env.VITE_APP_NAME}</p>
      <p className="text-gray-600">API: {import.meta.env.VITE_API_BASE_URL}</p>
    </div>
  )
}
